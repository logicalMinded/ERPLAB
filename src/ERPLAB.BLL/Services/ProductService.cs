using ERPLAB.DataAccess.Core;
using ERPLAB.DataAccess.Repositories;
using ERPLAB.Models.Constants;
using ERPLAB.Models.Entities;
using ERPLAB.Models.Exceptions;
using Microsoft.Data.SqlClient;

namespace ERPLAB.BLL.Services
{
    /// <summary>
    /// 商品基本檔商業邏輯服務 (BLL)
    /// 負責處理商品實體之業務規則、狀態流轉與單號配發，
    /// 作為展示層 (UI) 與資料存取層 (DAL) 之間的隔離邊界。
    /// </summary>
    public class ProductService
    {
        private readonly ProductRepository _repo;

        public ProductService()
        {
            _repo = new ProductRepository();
        }

        // =====================================================================
        // 資料讀取服務 (Query)
        // =====================================================================

        /// <summary>
        /// 依條件分頁取得商品清單
        /// </summary>
        public async Task<(List<Product> Items, int TotalCount)> GetProductsAsync(int pageNumber, int pageSize, bool includeInactive = false, string keyword = "")
        {
            return await _repo.GetProductsAsync(pageNumber, pageSize, includeInactive, keyword);
        }

        // =====================================================================
        // 交易服務：建立商品基本檔 (Command)
        // =====================================================================

        /// <summary>
        /// 建立商品基本檔
        /// </summary>
        /// <param name="product">商品實體</param>
        /// <param name="accountID">操作者帳號 ID</param>
        /// <returns>建立完成的商品實體 (包含資料庫配發之主鍵與樂觀鎖)</returns>
        public async Task<Product> CreateProductAsync(Product product, int accountID)
        {
            // 初始化業務狀態：確保新商品預設為上架狀態且庫存為零
            product.CurrentStock = 0;
            product.IsActive = true;
            product.CreateUser = accountID;

            try
            {
                // 委由共用模組配發唯一的業務流水號
                product.ProductNo = await AutoNumberHelper.GetNextSequenceAsync(AutoNumberPrefixes.Product);

                return await _repo.CreateAsync(product);
            }
            catch (SqlException ex)
            {
                // 攔截並轉譯資料庫層級的例外，維持 BLL 對上層的介面合約一致性
                throw TranslateSqlException(ex);
            }

        }

        // =====================================================================
        // 交易服務：更新商品基本檔 (Command)
        // =====================================================================

        /// <summary>
        /// 更新商品基本檔
        /// </summary>
        public async Task<byte[]> UpdateProductAsync(Product product, int accountID)
        {
            product.CreateUser = accountID;
            try
            {
                return await _repo.UpdateAsync(product);
            }
            catch (SqlException ex)
            {
                throw TranslateSqlException(ex);

            }
        }

        // =====================================================================
        // 狀態機服務：切換商品上下架狀態 (State Transition)
        // =====================================================================

        /// <summary>
        /// 獨立切換商品啟用/停用狀態，確保狀態變更不夾帶一般資料欄位的修改
        /// </summary>
        public async Task<byte[]> UpdateProductStatusAsync(int productId, bool currentStatus, byte[] rowVersion, int updateUser)
        {
            // 狀態反轉邏輯收斂於此
            bool targetStatus = !currentStatus;
            try
            {
                return await _repo.UpdateStatusAsync(productId, targetStatus, rowVersion, updateUser);
            }
            catch (SqlException ex)
            {
                throw TranslateSqlException(ex);
            }
        }

        /// <summary>
        /// 依業務編號精確查詢單一商品實體
        /// </summary>
        public async Task<Product?> GetProductByNoAsync(string productNo)
        {
            return await _repo.GetProductByNoAsync(productNo);
        }

        // =====================================================================
        // 例外轉譯處理 (Exception Translation)
        // =====================================================================

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
                friendlyMsg = "系統拒絕存檔：商品編號不可與現有資料重複！";
            }
            // 處理 Check Constraint 違反限制
            if (sqlex.Number == 547)
            {
                // 針對商品檔特有的 CHECK 約束進行業務語意轉譯
                if (sqlex.Message.Contains("CK_Product_PurchasePrice") || sqlex.Message.Contains("CK_Product_SalesPrice"))
                {
                    friendlyMsg = "系統拒絕存檔：參考進貨單價與常態售價不可小於 0！";
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