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
    /// 進貨單商業邏輯服務 (BLL 大腦)。
    /// 核心職責：隔離 UI 與 DAL，負責零信任算力 (總金額)、單號配發、審計足跡烙印與狀態機邏輯檢核。
    /// </summary>
    public class PurchaseOrderService
    {
        private readonly PurchaseRepository _repo;

        public PurchaseOrderService()
        {
            _repo = new PurchaseRepository();
        }

        // =====================================================================
        // 🔍 讀取服務
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
        // ➕ 核心交易：建立進貨草稿
        // =====================================================================
        public async Task<PurchaseMaster> CreatePurchaseOrderAsync(PurchaseMaster master, List<PurchaseDetail> details, int currentAccountId)
        {
            // 零信任計算：由後端強迫重算總金額
            master.RecalculateTotalAmount(details);

            // 狀態初始化
            master.Status = (byte)DocumentStatus.Draft;

            // 審計足跡烙印 (統一由 BLL 寫入)
            master.CreateUser = currentAccountId;
            master.UpdateUser = currentAccountId;

            try
            {
                //獨立微交易取號
                master.PurchaseNo = await AutoNumberHelper.GetNextSequenceAsync(AutoNumberPrefixes.PurchaseOrder);

                return await _repo.CreatePurchaseOrderAsync(master, details);
            }
            catch (SqlException sqlex)
            {
                // 🛡️ 例外轉譯：將 DB 錯誤化為純粹的商業語意
                if (sqlex.Number == 2627 || sqlex.Number == 2601)
                    throw new BusinessRuleException("系統拒絕存檔：進貨單號發生不可預期的重複！");
                else if (sqlex.Number == 547)
                    throw new BusinessRuleException("系統拒絕存檔：資料格式違反底層限制！");

                throw new BusinessRuleException($"資料庫寫入異常：{sqlex.Message}");
            }
        }

        // =====================================================================
        // 📝 核心交易：更新進貨草稿
        // =====================================================================
        public async Task<byte[]> UpdatePurchaseOrderDraftAsync(PurchaseMaster master, List<PurchaseDetail> details, int currentAccountId)
        {
            // 物理阻斷：非草稿狀態絕對不允許修改
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
        // 🔐 核心交易：狀態機推進 (過帳 / 作廢)
        // =====================================================================
        public async Task<byte[]> ChangeOrderStatusAsync(long purchaseId, byte currentStatus, byte targetStatus, byte[] rowVersion, int currentAccountId)
        {
            // 合法性矩陣防護
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
                // 💡 [領域專屬轉譯] 進貨單的作廢會「扣除庫存」，若已被賣掉會觸發 547 錯誤！
                if (sqlex.Number == 547 && sqlex.Message.Contains("CK_Product_CurrentStock"))
                {
                    throw new BusinessRuleException("作廢失敗：該批進貨商品已被售出或耗用，導致「庫存餘額不足」，系統物理拒絕作廢沖銷！");
                }

                throw new BusinessRuleException($"狀態變更時違反資料庫底層限制：{sqlex.Message}");
            }
        }
    }
}