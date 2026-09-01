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
    /// 盤點單商業邏輯服務 (BLL 大腦)。
    /// 核心職責：物理刪除授權、庫存快照檢核、單號配發與審計足跡烙印。
    /// </summary>
    public class InventoryService
    {
        private readonly InventoryRepository _repo;

        public InventoryService()
        {
            _repo = new InventoryRepository();
        }

        // =====================================================================
        // 🔍 讀取服務
        // =====================================================================
        public async Task<(List<InventoryMaster> Items, int TotalCount)> GetInventoryOrdersAsync(int pageNumber, int pageSize, string keyword = "")
        {
            // 💡 盤點單無 IsActive，也無作廢狀態，故不需 showVoided 參數
            return await _repo.GetInventoryOrdersAsync(pageNumber, pageSize, keyword);
        }

        public async Task<List<InventoryDetail>> GetInventoryDetailsAsync(long inventoryId)
        {
            return await _repo.GetInventoryDetailsAsync(inventoryId);
        }

        // =====================================================================
        // ➕ 核心交易：建立盤點草稿
        // =====================================================================
        public async Task<InventoryMaster> CreateInventoryOrderAsync(InventoryMaster master, List<InventoryDetail> details, int currentAccountId)
        {
            // 狀態初始化：強制為草稿
            master.Status = (byte)DocumentStatus.Draft;

            // 審計足跡烙印
            master.CreateUser = currentAccountId;
            master.UpdateUser = currentAccountId;

            try
            {
                // 獨立微交易取號
                master.InventoryNo = await AutoNumberHelper.GetNextSequenceAsync(AutoNumberPrefixes.InventoryCheck);
                return await _repo.CreateInventoryOrderAsync(master, details);
            }
            catch (SqlException sqlex)
            {
                // 💡 例外轉譯：攔截 SQL 錯誤，翻譯為純淨的業務語言
                if (sqlex.Number == 2627 || sqlex.Number == 2601)
                    throw new BusinessRuleException("系統拒絕存檔：盤點單號發生不可預期的重複！");
                else if (sqlex.Number == 547)
                    throw new BusinessRuleException("系統拒絕存檔：資料格式違反底層限制！");

                throw new BusinessRuleException($"資料庫寫入異常：{sqlex.Message}");
            }
        }

        // =====================================================================
        // 📝 核心交易：更新盤點草稿
        // =====================================================================
        public async Task<byte[]> UpdateInventoryOrderDraftAsync(InventoryMaster master, List<InventoryDetail> details, int currentAccountId)
        {
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
        // 🗑️ 核心交易：物理刪除草稿 (Hard Delete)
        // =====================================================================
        public async Task DeleteDraftAsync(long inventoryId, byte[] rowVersion, byte status)
        {
            if (status != (byte)DocumentStatus.Draft)
                throw new InvalidOperationException("商業邏輯違規：非草稿狀態之盤點單嚴禁刪除！");
            try
            {
                // 物理刪除交由 DAL 與 DB Trigger 把關狀態
                await _repo.DeleteDraftAsync(inventoryId, rowVersion);
            }
            catch (SqlException sqlex)
            {
                throw new BusinessRuleException($"刪除失敗，資料可能具備關聯限制：{sqlex.Message}");
            }
        }

        // =====================================================================
        // 🔐 核心交易：狀態機推進 (審核過帳)
        // =====================================================================
        public async Task<byte[]> ApproveOrderAsync(long inventoryId, byte[] rowVersion, byte status, int currentAccountId)
        {
            // 💡 盤點單專屬商業規則：只允許 1 -> 2 (草稿變過帳)，絕對不允許作廢或退回！
            if (status != (byte)DocumentStatus.Draft)
                throw new InvalidOperationException("商業邏輯違規：非草稿狀態之盤點單嚴禁過帳！");

            try
            {
                return await _repo.UpdateOrderStatusAsync(inventoryId, rowVersion, currentAccountId);
            }
            catch (SqlException sqlex)
            {
                if (sqlex.Number == 547 && sqlex.Message.Contains("CK_Product_CurrentStock"))
                {
                    throw new BusinessRuleException("過帳失敗：部分商品「帳面庫存不足以扣抵盤虧」，系統物理拒絕過帳！");
                }

                throw new BusinessRuleException($"狀態變更時違反資料庫底層限制：{sqlex.Message}");
            }
        }

    }
}