using ERPLAB.DataAccess.Core;
using ERPLAB.DataAccess.Repositories;
using ERPLAB.Models.Constants;
using ERPLAB.Models.Entities;
using ERPLAB.Models.Exceptions;
using Microsoft.Data.SqlClient;

namespace ERPLAB.BLL.Services
{
    /// <summary>
    /// 客戶基本檔商業邏輯服務 (BLL)
    /// 負責處理客戶資料的商業規則，包含自動編碼配發、審計軌跡 (Audit Trail) 維護、
    /// 狀態流轉，以及底層資料存取異常的轉譯，作為展示層與資料存取層之間的隔離邊界。
    /// </summary>
    public class CustomerService
    {
        private readonly CustomerRepository _repo;

        public CustomerService()
        {
            _repo = new CustomerRepository();
        }

        /// <summary>
        /// 依條件分頁取得客戶清單
        /// </summary>
        public async Task<(List<Customer> Items, int TotalCount)> GetCustomersAsync(int pageNumber, int pageSize, bool includeInactive = false, string keyword = "")
        {
            return await _repo.GetCustomersAsync(pageNumber, pageSize, includeInactive, keyword);
        }

        /// <summary>
        /// 建立客戶基本檔
        /// </summary>
        /// <param name="entity">客戶實體</param>
        /// <param name="accountId">操作者帳號 ID</param>
        /// <returns>建立完成的客戶實體 (包含資料庫配發之主鍵與樂觀鎖)</returns>
        public async Task<Customer> CreateCustomerAsync(Customer entity, int accountId)
        {
            // 寫入初始業務狀態與審計軌跡
            entity.IsActive = true;
            entity.CreateUser = accountId;
            entity.UpdateUser = accountId;

            try
            {
                // 委由共用模組配發唯一的業務流水號
                entity.CustomerNo = await AutoNumberHelper.GetNextSequenceAsync(AutoNumberPrefixes.Customer);

                return await _repo.CreateAsync(entity);
            }
            catch (SqlException sqlex)
            {
                // 攔截並轉譯資料庫層級的例外，以維持 BLL 對上層的介面合約一致性
                throw TranslateSqlException(sqlex);
            }
        }

        /// <summary>
        /// 更新客戶基本檔
        /// </summary>
        public async Task<byte[]> UpdateCustomerAsync(Customer entity, int accountId)
        {
            // 更新審計軌跡
            entity.UpdateUser = accountId;
            try
            {
                return await _repo.UpdateAsync(entity);
            }
            catch (SqlException sqlex)
            {
                throw TranslateSqlException(sqlex);
            }
        }

        /// <summary>
        /// 切換客戶啟用/停用狀態
        /// </summary>
        public async Task<byte[]> ToggleCustomerStatusAsync(int customerId, bool currentStatus, byte[] rowVersion, int accountId)
        {
            // 狀態反轉邏輯收斂於此，確保資料存取層僅負責單純的狀態覆寫
            bool targetStatus = !currentStatus;
            try
            {
                return await _repo.UpdateStatusAsync(customerId, targetStatus, rowVersion, accountId);
            }
            catch (SqlException sqlex)
            {
                throw TranslateSqlException(sqlex);
            }
        }

        /// <summary>
        /// 將 SqlException 轉譯為具備商業語意的 BusinessRuleException。
        /// 避免底層資料庫實作細節 (如 Constraint Name) 外洩至展示層，確保架構的封閉性。
        /// </summary>
        private BusinessRuleException TranslateSqlException(SqlException sqlex)
        {
            string friendlyMsg = $"資料庫寫入異常(代碼：{sqlex.Number})，請聯絡系統管理員進行查修。";

            // 處理 Unique Key 違反限制
            if (sqlex.Number == 2627 || sqlex.Number == 2601)
            {
                friendlyMsg = "系統拒絕存檔：客戶編號、電話或統一編號不可與現有資料重複！";
            }
            // 處理 Check Constraint 違反限制
            else if (sqlex.Number == 547)
            {
                if (sqlex.Message.Contains("CK_Customer_CustomZipCode_Length") || sqlex.Message.Contains("CK_Customer_CustomZipCode_Numeric")
                    || sqlex.Message.Contains("CK_Customer_Email_NullableCheck") || sqlex.Message.Contains("CK_Customerr_PhoneNumber_StrictSymbols")
                    || sqlex.Message.Contains("CK_Customer_TaxID_Numeric"))
                {
                    friendlyMsg = "系統拒絕存檔：資料格式違反底層限制 (統一編號、電子信箱、電話或郵遞區號格式不符)！";
                }
                else
                {
                    friendlyMsg = "系統拒絕存檔：資料格式違反底層限制！";
                }
            }

            return new BusinessRuleException(friendlyMsg);
        }
    }
}