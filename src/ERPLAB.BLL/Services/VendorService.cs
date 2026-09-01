using ERPLAB.DataAccess.Core;
using ERPLAB.DataAccess.Repositories;
using ERPLAB.Models.Constants;
using ERPLAB.Models.Entities;
using ERPLAB.Models.Exceptions;
using Microsoft.Data.SqlClient;

namespace ERPLAB.BLL.Services
{
    /// <summary>
    /// 廠商基本檔商業邏輯服務 (BLL 大腦)。
    /// 核心職責：隔離 UI 與 DAL，集中處理取號邏輯與狀態機分流。
    /// </summary>
    public class VendorService
    {
        private readonly VendorRepository _repo;

        public VendorService()
        {
            _repo = new VendorRepository();
        }

        // =====================================================================
        // 🔍 讀取服務 (純粹的代理呼叫)
        // =====================================================================
        public async Task<(List<Vendor> Items, int TotalCount)> GetVendorsAsync(int pageNumber, int pageSize, bool includeInactive = false, string keyword = "")
        {
            return await _repo.GetVendorsAsync(pageNumber, pageSize, includeInactive, keyword);
        }

        // =====================================================================
        // ➕ 核心交易：新增廠商
        // =====================================================================
        public async Task<Vendor> CreateVendorAsync(Vendor vendor, int accountId)
        {
            vendor.IsActive = true;
            vendor.CreateUser = accountId;
            vendor.UpdateUser = accountId;
            try
            {
                // 呼叫底層微交易取號引擎
                vendor.VendorNo = await AutoNumberHelper.GetNextSequenceAsync(AutoNumberPrefixes.Vendor);

                return await _repo.CreateAsync(vendor);
            }
            catch (SqlException sqlex)
            {
                throw TranslateSqlException(sqlex);
            }
        }

        // =====================================================================
        // 📝 核心交易：更新廠商資料
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
        // 🔄 核心交易：變更廠商狀態 (停用/復權)
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
        // 🛡️ [例外轉譯器] 將骯髒的 SQL 代碼化為純潔的商業語言
        // =====================================================================
        private BusinessRuleException TranslateSqlException(SqlException sqlex)
        {
            string friendlyMsg = $"資料庫寫入異常(代碼：{sqlex.Number})，請聯絡系統管理員進行查修。";
            if (sqlex.Number == 2627 || sqlex.Number == 2601)
            {
                // 💡 精確對應 Vendor 的業務情境
                friendlyMsg = "系統拒絕存檔：廠商編號、電話或統一編號不可與現有資料重複！";
            }
            else if (sqlex.Number == 547)
            {
                if (sqlex.Message.Contains("CK_Vendor_CustomZipCode_Length") || sqlex.Message.Contains("CK_Vendor_CustomZipCode_Numeric")
                    || sqlex.Message.Contains("CK_Vendor_Email_NullableCheck") || sqlex.Message.Contains("CK_Vendor_PhoneNumber_StrictSymbols")
                    || sqlex.Message.Contains("CK_Vendor_TaxID_Numeric"))
                { friendlyMsg = "系統拒絕存檔：資料格式違反底層限制 (統一編號、電子信箱、電話或郵遞區號格式不符)！"; }
                else
                {
                    friendlyMsg = "系統拒絕存檔：資料格式違反底層限制！";
                }
            }

            // 其他 SQL 異常也包裝起來，避免底層 StackTrace 直接暴露給 UI
            return new BusinessRuleException(friendlyMsg);
        }
    }
}