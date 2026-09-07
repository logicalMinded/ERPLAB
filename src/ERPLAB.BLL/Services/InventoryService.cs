using ERPLAB.DataAccess.Core;
using ERPLAB.DataAccess.Repositories;
using ERPLAB.Models.Constants;
using ERPLAB.Models.Entities;
using ERPLAB.Models.Enums;
using ERPLAB.Models.Exceptions;
using Microsoft.Data.SqlClient;

namespace ERPLAB.BLL.Services
{
    /// <summary>
    /// 庫存盤點作業之商業邏輯服務 (BLL)
    /// 負責處理盤點單的狀態機流轉、實體刪除驗證、單號配發，
    /// 並將底層資料存取異常轉譯為前端可識別的商業規則例外 (BusinessRuleException)。
    /// </summary>
    public class InventoryService
    {
        private readonly InventoryRepository _repo;

        public InventoryService()
        {
            _repo = new InventoryRepository();
        }

        // =====================================================================
        // 資料讀取服務 (Query)
        // =====================================================================
        public async Task<(List<InventoryMaster> Items, int TotalCount)> GetInventoryOrdersAsync(int pageNumber, int pageSize, string keyword = "")
        {
            // 盤點單的業務特性為物理刪除且無作廢流程，因此不需實作歷史狀態 (如 showVoided) 的過濾參數
            return await _repo.GetInventoryOrdersAsync(pageNumber, pageSize, keyword);
        }

        public async Task<List<InventoryDetail>> GetInventoryDetailsAsync(long inventoryId)
        {
            return await _repo.GetInventoryDetailsAsync(inventoryId);
        }

        // =====================================================================
        // 交易服務：建立盤點草稿 (Command)
        // =====================================================================
        public async Task<InventoryMaster> CreateInventoryOrderAsync(InventoryMaster master, List<InventoryDetail> details, int currentAccountId)
        {
            // 初始業務狀態：限制新建單據必為草稿 (Draft)
            master.Status = (byte)DocumentStatus.Draft;

            // 寫入審計軌跡 (Audit Trail)
            master.CreateUser = currentAccountId;
            master.UpdateUser = currentAccountId;

            try
            {
                // 呼叫共用服務配發業務流水號，此動作與主交易隔離以避免鎖定爭用 (Lock Contention)
                master.InventoryNo = await AutoNumberHelper.GetNextSequenceAsync(AutoNumberPrefixes.InventoryCheck);
                return await _repo.CreateInventoryOrderAsync(master, details);
            }
            catch (SqlException sqlex)
            {
                // 例外轉譯 (Exception Translation)：將底層約束衝突封裝為商業邏輯例外，維持架構邊界
                if (sqlex.Number == 2627 || sqlex.Number == 2601)
                    throw new BusinessRuleException("系統拒絕存檔：盤點單號發生不可預期的重複！");
                else if (sqlex.Number == 547)
                    throw new BusinessRuleException("系統拒絕存檔：資料格式違反底層限制！");

                throw new BusinessRuleException($"資料庫寫入異常：{sqlex.Message}");
            }
        }

        // =====================================================================
        // 交易服務：更新盤點草稿 (Command)
        // =====================================================================
        public async Task<byte[]> UpdateInventoryOrderDraftAsync(InventoryMaster master, List<InventoryDetail> details, int currentAccountId)
        {
            // 防衛語句 (Guard Clause)：確保非草稿狀態之單據不可異動
            if (master.Status != (byte)DocumentStatus.Draft)
                throw new InvalidOperationException("商業邏輯違規：非草稿狀態之盤點單嚴禁修改！");

            master.UpdateUser = currentAccountId;
            try
            {
                return await _repo.UpdateInventoryOrderDraftAsync(master, details);
            }
            catch (SqlException sqlex)
            {
                if (sqlex.Number == 547) throw new BusinessRuleException("系統拒絕更新：資料格式違反底層限制！");
                throw new BusinessRuleException($"資料庫更新異常：{sqlex.Message}");
            }
        }

        // =====================================================================
        // 交易服務：物理刪除盤點草稿 (Hard Delete)
        // =====================================================================
        public async Task DeleteDraftAsync(long inventoryId, byte[] rowVersion, byte status)
        {
            // 狀態防禦：僅允許草稿狀態執行物理刪除
            if (status != (byte)DocumentStatus.Draft)
                throw new InvalidOperationException("商業邏輯違規：非草稿狀態之盤點單嚴禁刪除！");
            try
            {
                // 實體刪除操作，依賴資料庫層級的關聯約束與 Trigger 確保資料一致性
                await _repo.DeleteDraftAsync(inventoryId, rowVersion);
            }
            catch (SqlException sqlex)
            {
                throw new BusinessRuleException($"刪除失敗，資料可能具備關聯限制：{sqlex.Message}");
            }
        }

        // =====================================================================
        // 狀態機服務：審核過帳 (State Transition)
        // =====================================================================
        public async Task<byte[]> ApproveOrderAsync(long inventoryId, byte[] rowVersion, byte status, int currentAccountId)
        {
            // 狀態機單向推進規則：盤點過帳具備不可逆性，僅允許 Draft -> Posted，不提供作廢流程
            if (status != (byte)DocumentStatus.Draft)
                throw new InvalidOperationException("商業邏輯違規：非草稿狀態之盤點單嚴禁過帳！");

            try
            {
                return await _repo.UpdateOrderStatusAsync(inventoryId, rowVersion, currentAccountId);
            }
            catch (SqlException sqlex)
            {
                // 攔截 CHECK Constraint 異常，轉譯為庫存餘額不足的商業警告
                if (sqlex.Number == 547 && sqlex.Message.Contains("CK_Product_CurrentStock"))
                {
                    throw new BusinessRuleException("過帳失敗：部分商品「帳面庫存不足以扣抵盤虧」，系統物理拒絕過帳！");
                }

                throw new BusinessRuleException($"狀態變更時違反資料庫底層限制：{sqlex.Message}");
            }
        }

    }
}