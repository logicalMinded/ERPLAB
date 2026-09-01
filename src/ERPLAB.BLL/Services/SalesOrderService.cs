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
    /// 銷貨單商業邏輯服務 (BLL 大腦)。
    /// 核心職責：隔離 UI 與 DAL，負責零信任算力 (總金額)、單號配發與狀態機邏輯檢核。
    /// </summary>
    public class SalesOrderService
    {
        private readonly SalesRepository _repo;

        public SalesOrderService()
        {
            _repo = new SalesRepository();
        }

        // =====================================================================
        // 🔍 讀取服務 (代理呼叫 DAL)
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
        // ➕ 核心交易：建立新單據
        // =====================================================================
        public async Task<SalesMaster> CreateSalesOrderAsync(SalesMaster master, List<SalesDetail> details, int currentAccountId)
        {
            // 零信任計算：絕對不信任前端 UI 顯示的金額，後端強迫重算快取欄位
            master.RecalculateTotalAmount(details);

            // 狀態初始化：新單據必定為草稿
            master.Status = (byte)DocumentStatus.Draft;

            // 審計足跡烙印
            master.CreateUser = currentAccountId;
            master.UpdateUser = currentAccountId;

            // 規則檢核通過，發包給 DAL 搬運工進行物理寫入 (SqlTransaction + TVP)
            try
            {
                // 單號配發：呼叫共用引擎，瞬間取得單號 (物理隔離，不佔用主交易鎖)
                master.SalesNo = await AutoNumberHelper.GetNextSequenceAsync(AutoNumberPrefixes.SalesOrder);

                return await _repo.CreateSalesOrderAsync(master, details);
            }
            catch (SqlException sqlex)
            {
                // 💡 異常轉譯引擎：將骯髒的 SQL 錯誤化為純潔的業務例外
                if (sqlex.Number == 2627 || sqlex.Number == 2601)
                    throw new BusinessRuleException("系統拒絕存檔：單據編號發生不可預期的重複！");
                else if (sqlex.Number == 547)
                    throw new BusinessRuleException("系統拒絕存檔：資料格式違反底層限制 (如郵遞區號長度錯誤)！");

                throw new BusinessRuleException($"資料庫寫入異常：{sqlex.Message}");
            }
        }

        // =====================================================================
        // 📝 核心交易：更新單據草稿
        // =====================================================================
        public async Task<byte[]> UpdateSalesOrderDraftAsync(SalesMaster master, List<SalesDetail> details, int currentAccountId)
        {
            // 💡 [商業規則 1] 邏輯防線：非草稿絕對不允許修改
            if (master.Status != (byte)DocumentStatus.Draft)
            {
                throw new InvalidOperationException("商業邏輯違規：非草稿狀態之單據嚴禁修改！");
            }

            // 💡 [商業規則 2] 零信任計算
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
        // 🔐 核心交易：狀態機推進 (過帳 / 作廢)
        // =====================================================================
        public async Task<byte[]> UpdateOrderStatusAsync(long salesId, byte currentStatus, byte targetStatus, byte[] rowVersion, int currentAccountId)
        {
            // 💡 [商業規則] 狀態流轉的合法性矩陣防護
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
                    // 💡 精準攔截庫存不足防線
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