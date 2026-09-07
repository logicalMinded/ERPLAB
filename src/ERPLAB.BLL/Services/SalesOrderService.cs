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
    /// 銷貨單商業邏輯服務 (BLL)
    /// 負責處理銷貨單的狀態機流轉、金額重算與驗證、單號配發，
    /// 並將底層資料存取異常轉譯為前端可識別的商業規則例外 (BusinessRuleException)。
    /// </summary>
    public class SalesOrderService
    {
        private readonly SalesRepository _repo;

        public SalesOrderService()
        {
            _repo = new SalesRepository();
        }

        // =====================================================================
        // 資料讀取服務 (Query)
        // =====================================================================
        public async Task<(List<SalesMaster> Items, int TotalCount)> GetSalesOrdersAsync(int pageNumber, int pageSize, string keyword = "", bool showVoided = false)
        {
            return await _repo.GetSalesOrdersAsync(pageNumber, pageSize, keyword, showVoided);
        }

        public async Task<List<SalesDetail>> GetSalesDetailsAsync(long salesId)
        {
            return await _repo.GetSalesDetailsAsync(salesId);
        }

        // =====================================================================
        // 交易服務：建立銷貨單 (Command)
        // =====================================================================
        public async Task<SalesMaster> CreateSalesOrderAsync(SalesMaster master, List<SalesDetail> details, int currentAccountId)
        {
            // 業務邏輯驗證：重新計算總金額，確保不依賴前端傳入之數值
            master.RecalculateTotalAmount(details);

            // 初始業務狀態：限制新建單據必為草稿 (Draft)
            master.Status = (byte)DocumentStatus.Draft;

            // 寫入審計軌跡 (Audit Trail)
            master.CreateUser = currentAccountId;
            master.UpdateUser = currentAccountId;

            try
            {
                // 呼叫共用服務配發業務流水號，此動作與主交易隔離以避免鎖定爭用 (Lock Contention)
                master.SalesNo = await AutoNumberHelper.GetNextSequenceAsync(AutoNumberPrefixes.SalesOrder);

                return await _repo.CreateSalesOrderAsync(master, details);
            }
            catch (SqlException sqlex)
            {
                // 例外轉譯 (Exception Translation)：將底層約束衝突封裝為商業邏輯例外，維持架構邊界
                if (sqlex.Number == 2627 || sqlex.Number == 2601)
                    throw new BusinessRuleException("系統拒絕存檔：單據編號發生不可預期的重複！");
                else if (sqlex.Number == 547)
                    throw new BusinessRuleException("系統拒絕存檔：資料格式違反底層限制 (如郵遞區號長度錯誤)！");

                throw new BusinessRuleException($"資料庫寫入異常：{sqlex.Message}");
            }
        }

        // =====================================================================
        // 交易服務：更新銷貨單草稿 (Command)
        // =====================================================================
        public async Task<byte[]> UpdateSalesOrderDraftAsync(SalesMaster master, List<SalesDetail> details, int currentAccountId)
        {
            // 防衛語句 (Guard Clause)：確保非草稿狀態之單據不可異動
            if (master.Status != (byte)DocumentStatus.Draft)
            {
                throw new InvalidOperationException("商業邏輯違規：非草稿狀態之單據嚴禁修改！");
            }

            // 重新計算總金額以確保資料一致性
            master.RecalculateTotalAmount(details);
            master.UpdateUser = currentAccountId;

            try
            {
                return await _repo.UpdateSalesOrderDraftAsync(master, details);
            }
            catch (SqlException sqlex)
            {
                if (sqlex.Number == 2627 || sqlex.Number == 2601)
                    throw new BusinessRuleException("系統拒絕存檔：單據編號發生不可預期的重複！");
                else if (sqlex.Number == 547)
                    throw new BusinessRuleException("系統拒絕存檔：資料格式違反底層限制！");

                throw new BusinessRuleException($"資料庫寫入異常：{sqlex.Message}");
            }
        }

        // =====================================================================
        // 狀態機服務：單據狀態推進 (State Transition)
        // =====================================================================
        public async Task<byte[]> UpdateOrderStatusAsync(long salesId, byte currentStatus, byte targetStatus, byte[] rowVersion, int currentAccountId)
        {
            // 狀態機流轉規則驗證：確保單據狀態轉換之合法性
            bool isValidTransition =
                (currentStatus == (byte)DocumentStatus.Draft && targetStatus == (byte)DocumentStatus.Posted) ||      // 審核過帳
                (currentStatus == (byte)DocumentStatus.Draft && targetStatus == (byte)DocumentStatus.Cancelled) ||   // 註銷草稿
                (currentStatus == (byte)DocumentStatus.Posted && targetStatus == (byte)DocumentStatus.Voided);       // 作廢沖銷

            if (!isValidTransition)
            {
                throw new InvalidOperationException($"商業邏輯違規：無法從狀態 [{currentStatus}] 流轉至狀態 [{targetStatus}]！");
            }

            try
            {
                return await _repo.UpdateOrderStatusAsync(salesId, currentStatus, targetStatus, rowVersion, currentAccountId);
            }
            catch (SqlException sqlex)
            {
                if (sqlex.Number == 547)
                {
                    // 攔截 CHECK Constraint 異常，轉譯為庫存餘額不足的商業警告
                    if (sqlex.Message.Contains("CK_Product_CurrentStock"))
                        throw new BusinessRuleException("過帳失敗：部分商品「庫存餘額不足」，系統物理拒絕扣帳出貨！");
                    else
                        throw new BusinessRuleException("狀態變更時違反資料庫底層限制！");
                }
                throw new BusinessRuleException($"狀態變更失敗：{sqlex.Message}");
            }
        }
    }
}