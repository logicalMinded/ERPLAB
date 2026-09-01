using ERPLAB.DataAccess.Core;
using ERPLAB.DataAccess.Repositories;
using ERPLAB.Models.Constants;
using ERPLAB.Models.Entities;
using ERPLAB.Models.Exceptions;
using Microsoft.Data.SqlClient;

namespace ERPLAB.BLL.Services
{
    /// <summary>
    /// 客戶基本檔商業邏輯服務 (BLL 大腦)
    /// 核心職責：封裝取號規則、審計足跡寫入與狀態機流轉，嚴禁 UI 直接觸碰 DAL。
    /// </summary>
    public class CustomerService
    {
        private readonly CustomerRepository _repo;

        public CustomerService()
        {
            _repo = new CustomerRepository();
        }

        // =====================================================================
        // 🔍 讀取服務
        // =====================================================================
        public async Task<(List<Customer> Items, int TotalCount)> GetCustomersAsync(int pageNumber, int pageSize, bool includeInactive = false, string keyword = "")
        {
            return await _repo.GetCustomersAsync(pageNumber, pageSize, includeInactive, keyword);
        }

        // =====================================================================
        // ➕ 新增服務 (封裝取號與預設值)
        // =====================================================================
        public async Task<Customer> CreateCustomerAsync(Customer entity, int accountId)
        {
            // 商業規則：新增時強制啟用、審計足跡
            entity.IsActive = true;
            entity.CreateUser = accountId;
            entity.UpdateUser = accountId;

            try
            {
                // 💡 商業邏輯：由 BLL 負責呼叫取號引擎
                entity.CustomerNo = await AutoNumberHelper.GetNextSequenceAsync(AutoNumberPrefixes.Customer);

                return await _repo.CreateAsync(entity);
            }
            catch (SqlException sqlex)
            {
                // 💡 捕捉底層例外，呼叫翻譯機，並由本方法親自 Throw 確保 Call Stack 清晰
                throw TranslateSqlException(sqlex);
            }
        }

        // =====================================================================
        // 📝 修改服務 (封裝審計足跡)
        // =====================================================================
        public async Task<byte[]> UpdateCustomerAsync(Customer entity, int accountId)
        {
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

        // =====================================================================
        // 🔄 狀態機切換服務
        // =====================================================================
        public async Task<byte[]> ToggleCustomerStatusAsync(int customerId, bool currentStatus, byte[] rowVersion, int accountId)
        {
            // 💡 商業規則：傳入當前狀態，BLL 負責將其反轉，再交給 DAL
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

        // =====================================================================
        // 🛡️ [錯誤轉譯引擎] 
        // =====================================================================
        private BusinessRuleException TranslateSqlException(SqlException sqlex)
        {
            string friendlyMsg = $"資料庫寫入異常(代碼：{sqlex.Number})，請聯絡系統管理員進行查修。";

            if (sqlex.Number == 2627 || sqlex.Number == 2601)
            {
                friendlyMsg = "系統拒絕存檔：客戶編號、電話或統一編號不可與現有資料重複！";
            }
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

            // 將翻譯好的訊息包裝成 BusinessRuleException 回傳
            return new BusinessRuleException(friendlyMsg);
        }
    }
}