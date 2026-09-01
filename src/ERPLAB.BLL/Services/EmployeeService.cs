using ERPLAB.DataAccess.Core;
using ERPLAB.DataAccess.Repositories;
using ERPLAB.Models.Constants;
using ERPLAB.Models.Entities;
using ERPLAB.Models.Enums;
using ERPLAB.Models.Exceptions;
using Microsoft.Data.SqlClient;
using System.Transactions;

namespace ERPLAB.BLL.Services
{
    /// <summary>
    /// 員工基本檔商業邏輯服務 (BLL 大腦)。
    /// 核心職責：隔離 UI 與 DAL，集中處理人事狀態機連動與取號邏輯。
    /// </summary>
    public class EmployeeService
    {
        private readonly EmployeeRepository _repo;
        private readonly AccountRepository _accountRepo;

        public EmployeeService()
        {
            _repo = new EmployeeRepository();
            _accountRepo = new AccountRepository();
        }

        // =====================================================================
        // 🔍 讀取服務 (代理呼叫 DAL)
        // =====================================================================
        public async Task<(List<Employee> Items, int TotalCount)> GetEmployeesAsync(int pageNumber, int pageSize, bool includeInactive = false, string keyword = "")
        {
            return await _repo.GetEmployeesAsync(pageNumber, pageSize, includeInactive, keyword);
        }

        // =====================================================================
        // ➕ 核心交易：新增員工
        // =====================================================================
        public async Task<Employee> CreateEmployeeAsync(Employee employee, int currentAccountId)
        {
            // 商業規則
            employee.IsActive = (employee.JobStatus == EmployeeJobStatus.Active);

            employee.CreateUser = currentAccountId;
            employee.UpdateUser = currentAccountId;

            try
            {
                // 商業規則：微交易取號引擎
                employee.EmployeeNo = await AutoNumberHelper.GetNextSequenceAsync(AutoNumberPrefixes.Employee);

                return await _repo.CreateAsync(employee);
            }
            catch (SqlException ex)
            {
                throw TranslateSqlException(ex);
            }
        }

        // =====================================================================
        // 📝 核心交易：更新員工資料
        // =====================================================================
        public async Task<byte[]> UpdateEmployeeAsync(Employee employee, int accountId)
        {
            // 商業規則 狀態機單向推導 (確保修改職務狀態時，系統權限同步物理封殺)
            employee.IsActive = (employee.JobStatus == EmployeeJobStatus.Active);

            employee.UpdateUser = accountId;

            try
            {
                // =====================================================================
                // 🛡️ [分散式交易防線] TransactionScope 
                // TransactionScopeAsyncFlowOption.Enabled 是非同步 (async/await) 交易的絕對必要參數！
                // =====================================================================
                using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

                // 2. 執行核心資料更新 (取得最新 RowVersion)
                byte[] newRowVersion = await _repo.UpdateAsync(employee);

                // =====================================================================
                // 💡 3. 跨模組商業連動 (Cross-Module Orchestration)
                // 商業鐵律：HR 將員工改為「離職/留停」時，全自動物理封殺其系統帳號。
                // (注意：若為復職，基於零信任資安，此處不自動啟用帳號，需由 IT 手動開啟)
                // =====================================================================
                if (!employee.IsActive)
                {
                    await _accountRepo.UpdateIsActiveByEmployeeIdAsync(
                        employee.EmployeeID,
                        isActive: false,
                        updateUser: accountId);
                }

                // 4. 宣告交易成功，一起 Commit 寫入硬碟
                scope.Complete();

                return newRowVersion;
            }
            catch (SqlException ex)
            {
                throw TranslateSqlException(ex);
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
                friendlyMsg = "系統拒絕存檔：員工工號或電子信箱不可與現有資料重複！";
            }
            else if (sqlex.Number == 547)
            {
                if (sqlex.Message.Contains("CK_Employee_CustomZipCode_Length") || sqlex.Message.Contains("CK_Employeer_CustomZipCode_Numeric")
                    || sqlex.Message.Contains("CK_Employee_Email_NullableCheck") || sqlex.Message.Contains("CK_Employee_PhoneNumber_StrictSymbols")
                    || sqlex.Message.Contains("CK_Employee_TaxID_Numeric"))
                {
                    friendlyMsg = "系統拒絕存檔：資料格式違反底層限制 (電子信箱、電話或郵遞區號格式不符)！";
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