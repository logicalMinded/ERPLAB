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
    /// 進貨單商業邏輯服務 (BLL)
    /// 負責處理進貨單之狀態流轉、金額重算、單號配發及審計軌跡維護，
    /// 作為展示層 (UI) 與資料存取層 (DAL) 之間的隔離邊界。
    /// </summary>
    public class PurchaseOrderService
    {
        private readonly PurchaseRepository _repo;

        public PurchaseOrderService()
        {
            _repo = new PurchaseRepository();
        }

        // =====================================================================
        // 資料讀取服務 (Query)
        // =====================================================================

        public async Task<(List<PurchaseMaster> Items, int TotalCount)> GetPurchaseOrdersAsync(int pageNumber, int pageSize, string keyword = "", bool showVoided = false)
        {
            return await _repo.GetPurchaseOrdersAsync(pageNumber, pageSize, keyword, showVoided);
        }

        public async Task<List<PurchaseDetail>> GetPurchaseDetailsAsync(long purchaseId)
        {
            return await _repo.GetPurchaseDetailsAsync(purchaseId);
        }

        // =====================================================================
        // 交易服務：建立進貨單草稿 (Command)
        // =====================================================================

        public async Task<PurchaseMaster> CreatePurchaseOrderAsync(PurchaseMaster master, List<PurchaseDetail> details, int currentAccountId)
        {
            // 強制重算總金額，避免依賴前端傳入之數值
            master.RecalculateTotalAmount(details);

            // 初始化業務狀態：預設為草稿 (Draft)
            master.Status = (byte)DocumentStatus.Draft;

            // 寫入審計軌跡 (Audit Trail)
            master.CreateUser = currentAccountId;
            master.UpdateUser = currentAccountId;

            try
            {
                // 委由共用模組配發業務流水號，與主交易隔離以減少鎖定爭用
                master.PurchaseNo = await AutoNumberHelper.GetNextSequenceAsync(AutoNumberPrefixes.PurchaseOrder);

                return await _repo.CreatePurchaseOrderAsync(master, details);
            }
            catch (SqlException sqlex)
            {
                // 例外轉譯 (Exception Translation)：將底層約束衝突封裝為商業規則例外
                if (sqlex.Number == 2627 || sqlex.Number == 2601)
                    throw new BusinessRuleException("系統拒絕存檔：進貨單號發生不可預期的重複！");
                else if (sqlex.Number == 547)
                    throw new BusinessRuleException("系統拒絕存檔：資料格式違反底層限制！");

                throw new BusinessRuleException($"資料庫寫入異常：{sqlex.Message}");
            }
        }

        // =====================================================================
        // 交易服務：更新進貨單草稿 (Command)
        // =====================================================================

        public async Task<byte[]> UpdatePurchaseOrderDraftAsync(PurchaseMaster master, List<PurchaseDetail> details, int currentAccountId)
        {
            // 防衛語句 (Guard Clause)：確保非草稿狀態之單據不可異動
            if (master.Status != (byte)DocumentStatus.Draft)
                throw new InvalidOperationException("商業邏輯違規：非草稿狀態之單據嚴禁修改！");

            master.RecalculateTotalAmount(details);
            master.UpdateUser = currentAccountId;

            try
            {
                return await _repo.UpdatePurchaseOrderDraftAsync(master, details);
            }
            catch (SqlException sqlex)
            {
                throw new BusinessRuleException($"進貨單草稿更新異常：{sqlex.Message}");
            }
        }

        // =====================================================================
        // 狀態機服務：單據狀態流轉 (State Transition)
        // =====================================================================

        public async Task<byte[]> ChangeOrderStatusAsync(long purchaseId, byte currentStatus, byte targetStatus, byte[] rowVersion, int currentAccountId)
        {
            // 狀態流轉合法性檢核 (State Transition Matrix)
            bool isValidTransition =
                (currentStatus == (byte)DocumentStatus.Draft && targetStatus == (byte)DocumentStatus.Posted) ||      // 審核過帳
                (currentStatus == (byte)DocumentStatus.Draft && targetStatus == (byte)DocumentStatus.Cancelled) ||   // 註銷草稿
                (currentStatus == (byte)DocumentStatus.Posted && targetStatus == (byte)DocumentStatus.Voided);       // 作廢沖銷

            if (!isValidTransition)
                throw new InvalidOperationException($"商業邏輯違規：無法從狀態 [{currentStatus}] 流轉至狀態 [{targetStatus}]！");

            try
            {
                return await _repo.UpdateOrderStatusAsync(purchaseId, currentStatus, targetStatus, rowVersion, currentAccountId);
            }
            catch (SqlException sqlex)
            {
                // 攔截 CHECK Constraint 異常。進貨單作廢將扣除實體庫存，若庫存不足將觸發底層限制，轉譯為具體商業警告。
                if (sqlex.Number == 547 && sqlex.Message.Contains("CK_Product_CurrentStock"))
                {
                    throw new BusinessRuleException("作廢失敗：該批進貨商品已被售出或耗用，導致「庫存餘額不足」，系統物理拒絕作廢沖銷！");
                }

                throw new BusinessRuleException($"狀態變更時違反資料庫底層限制：{sqlex.Message}");
            }
        }
    }
}