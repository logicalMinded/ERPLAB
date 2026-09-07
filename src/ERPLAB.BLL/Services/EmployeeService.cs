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
    /// 員工基本檔商業邏輯服務 (BLL)
    /// 負責處理員工資料之商業邏輯，包含自動取號、狀態機連動及跨倉儲(AccountRepository)的交易一致性。
    /// 作為展示層與資料存取層之間的隔離邊界。
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

        /// <summary>
        /// 依條件分頁取得員工清單
        /// </summary>
        public async Task<(List<Employee> Items, int TotalCount)> GetEmployeesAsync(int pageNumber, int pageSize, bool includeInactive = false, string keyword = "")
        {
            return await _repo.GetEmployeesAsync(pageNumber, pageSize, includeInactive, keyword);
        }

        /// <summary>
        /// 新增員工資料
        /// </summary>
        /// <param name="employee">員工實體</param>
        /// <param name="currentAccountId">操作者帳號 ID</param>
        /// <returns>建立完成的員工實體 (包含資料庫配發之主鍵與樂觀鎖)</returns>
        public async Task<Employee> CreateEmployeeAsync(Employee employee, int currentAccountId)
        {
            // 依據人事狀態 (JobStatus) 推導系統啟用狀態 (IsActive)
            employee.IsActive = (employee.JobStatus == EmployeeJobStatus.Active);

            employee.CreateUser = currentAccountId;
            employee.UpdateUser = currentAccountId;

            try
            {
                // 委由共用模組配發唯一的業務流水號
                employee.EmployeeNo = await AutoNumberHelper.GetNextSequenceAsync(AutoNumberPrefixes.Employee);

                return await _repo.CreateAsync(employee);
            }
            catch (SqlException ex)
            {
                throw TranslateSqlException(ex);
            }
        }

        /// <summary>
        /// 更新員工資料，並處理關聯帳號的狀態連動
        /// </summary>
        public async Task<byte[]> UpdateEmployeeAsync(Employee employee, int accountId)
        {
            // 依據人事狀態推導系統啟用狀態，確保修改職務狀態時，系統權限同步調整
            employee.IsActive = (employee.JobStatus == EmployeeJobStatus.Active);

            employee.UpdateUser = accountId;

            try
            {
                // 宣告非同步交易範圍 (TransactionScope)，確保跨倉儲寫入作業的 ACID 特性。
                // 必須傳入 TransactionScopeAsyncFlowOption.Enabled 以支援 async/await 執行緒切換。
                using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

                byte[] newRowVersion = await _repo.UpdateAsync(employee);

                // 跨模組業務連動 (Cross-Module Orchestration)：
                // 若員工變更為非在職狀態，系統將自動停用其關聯之登入帳號。
                // 註：基於最小權限原則，復職時不自動恢復帳號，需經由系統管理員手動啟用。
                if (!employee.IsActive)
                {
                    await _accountRepo.UpdateIsActiveByEmployeeIdAsync(
                        employee.EmployeeID,
                        isActive: false,
                        updateUser: accountId);
                }

                // 確認交易完成
                scope.Complete();

                return newRowVersion;
            }
            catch (SqlException ex)
            {
                throw TranslateSqlException(ex);
            }
        }

        /// <summary>
        /// 將 SqlException 轉譯為具備商業語意的 BusinessRuleException。
        /// 避免底層資料庫實作細節外洩至展示層，確保架構的封閉性。
        /// </summary>
        private BusinessRuleException TranslateSqlException(SqlException sqlex)
        {
            string friendlyMsg = $"資料庫寫入異常(代碼：{sqlex.Number})，請聯絡系統管理員進行查修。";

            // 處理 Unique Key 違反限制
            if (sqlex.Number == 2627 || sqlex.Number == 2601)
            {
                friendlyMsg = "系統拒絕存檔：員工工號或電子信箱不可與現有資料重複！";
            }
            // 處理 Check Constraint 違反限制
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