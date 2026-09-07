using ERPLAB.DataAccess.Core;
using ERPLAB.DataAccess.Repositories;
using ERPLAB.Models.Constants;
using ERPLAB.Models.Entities;
using ERPLAB.Models.Exceptions;
using Microsoft.Data.SqlClient;
namespace ERPLAB.BLL.Services
{
    /// <summary>
    /// 廠商基本檔商業邏輯服務 (BLL)
    /// 負責處理廠商資料的建立、更新、狀態切換及單號配發，
    /// 並將底層資料存取異常轉譯為前端可識別的商業規則例外 (BusinessRuleException)。
    /// </summary>
    public class VendorService
    {
        private readonly VendorRepository _repo;

        public VendorService()
        {
            _repo = new VendorRepository();
        }

        // =====================================================================
        // 資料讀取服務 (Query)
        // =====================================================================
        public async Task<(List<Vendor> Items, int TotalCount)> GetVendorsAsync(int pageNumber, int pageSize, bool includeInactive = false, string keyword = "")
        {
            return await _repo.GetVendorsAsync(pageNumber, pageSize, includeInactive, keyword);
        }

        // =====================================================================
        // 交易服務：新增廠商 (Command)
        // =====================================================================
        public async Task<Vendor> CreateVendorAsync(Vendor vendor, int accountId)
        {
            vendor.IsActive = true;
            vendor.CreateUser = accountId;
            vendor.UpdateUser = accountId;
            try
            {
                // 呼叫共用服務配發業務流水號
                vendor.VendorNo = await AutoNumberHelper.GetNextSequenceAsync(AutoNumberPrefixes.Vendor);

                return await _repo.CreateAsync(vendor);
            }
            catch (SqlException sqlex)
            {
                throw TranslateSqlException(sqlex);
            }
        }

        // =====================================================================
        // 交易服務：更新廠商資料 (Command)
        // =====================================================================
        public async Task<byte[]> UpdateVendorAsync(Vendor vendor, int accountId)
        {
            vendor.UpdateUser = accountId;
            try
            {
                return await _repo.UpdateAsync(vendor);
            }
            catch (SqlException sqlex)
            {
                throw TranslateSqlException(sqlex);
            }
        }

        // =====================================================================
        // 狀態機服務：變更廠商狀態 (停用/啟用)
        // =====================================================================
        public async Task<byte[]> UpdateVendorStatusAsync(int vendorId, bool currentStatus, byte[] rowVersion, int updateUser)
        {
            bool targetStatus = !currentStatus;
            try
            {
                return await _repo.UpdateStatusAsync(vendorId, targetStatus, rowVersion, updateUser);
            }
            catch (SqlException sqlex)
            {
                throw TranslateSqlException(sqlex);
            }
        }

        // =====================================================================
        // 輔助方法：SQL 例外轉譯 (Exception Translation)
        // =====================================================================
        private BusinessRuleException TranslateSqlException(SqlException sqlex)
        {
            string friendlyMsg = $"資料庫寫入異常(代碼：{sqlex.Number})，請聯絡系統管理員進行查修。";
            if (sqlex.Number == 2627 || sqlex.Number == 2601)
            {
                // 攔截 Unique Constraint 衝突，對應廠商基本檔的業務限制
                friendlyMsg = "系統拒絕存檔：廠商編號、電話或統一編號不可與現有資料重複！";
            }
            else if (sqlex.Number == 547)
            {
                if (sqlex.Message.Contains("CK_Vendor_CustomZipCode_Length") || sqlex.Message.Contains("CK_Vendor_CustomZipCode_Numeric")
                    || sqlex.Message.Contains("CK_Vendor_Email_NullableCheck") || sqlex.Message.Contains("CK_Vendor_PhoneNumber_StrictSymbols")
                    || sqlex.Message.Contains("CK_Vendor_TaxID_Numeric"))
                {
                    friendlyMsg = "系統拒絕存檔：資料格式違反底層限制 (統一編號、電子信箱、電話或郵遞區號格式不符)！";
                }
                else
                {
                    friendlyMsg = "系統拒絕存檔：資料格式違反底層限制！";
                }
            }

            // 將其他 SQL 異常封裝為商業邏輯例外，避免底層 StackTrace 暴露至前端
            return new BusinessRuleException(friendlyMsg);
        }
    }
}