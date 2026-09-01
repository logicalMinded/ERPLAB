using ERPLAB.DataAccess.Core;
using ERPLAB.DataAccess.Repositories;
using ERPLAB.Models.Constants;
using ERPLAB.Models.Entities;
using ERPLAB.Models.Exceptions;
using Microsoft.Data.SqlClient;

namespace ERPLAB.BLL.Services
{
    /// <summary>
    /// 商品基本檔商業邏輯服務 (BLL 大腦)。
    /// 核心職責：隔離 UI 與 DAL，集中處理商品狀態預設值與取號邏輯。
    /// </summary>
    public class ProductService
    {
        private readonly ProductRepository _repo;

        public ProductService()
        {
            _repo = new ProductRepository();
        }

        // =====================================================================
        // 🔍 讀取服務 (代理呼叫 DAL)
        // =====================================================================
        public async Task<(List<Product> Items, int TotalCount)> GetProductsAsync(int pageNumber, int pageSize, bool includeInactive = false, string keyword = "")
        {
            return await _repo.GetProductsAsync(pageNumber, pageSize, includeInactive, keyword);
        }

        // =====================================================================
        // ➕ 核心交易：新增商品
        // =====================================================================
        public async Task<Product> CreateProductAsync(Product product, int accountID)
        {
            // 商業規則：新增商品絕對預設為 0 庫存與上架狀態
            product.CurrentStock = 0;
            product.IsActive = true;
            product.CreateUser = accountID;

            try
            {
                // 💡 商業邏輯：交由後端大腦執行自動取號
                product.ProductNo = await AutoNumberHelper.GetNextSequenceAsync(AutoNumberPrefixes.Product);

                return await _repo.CreateAsync(product);
            }
            catch (SqlException ex)
            {
                // 💡 例外轉譯邊界：攔截底層物理錯誤，翻譯為純粹的商業邏輯例外 (BusinessRuleException)
                throw TranslateSqlException(ex);
            }

        }

        // =====================================================================
        // 📝 核心交易：更新商品資料
        // =====================================================================
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
        // 🔄 核心交易：獨立更新上下架狀態 (防夾帶修改)
        // =====================================================================
        public async Task<byte[]> UpdateProductStatusAsync(int productId, bool currentStatus, byte[] rowVersion, int updateUser)
        {
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
        public async Task<Product?> GetProductByNoAsync(string productNo)
        {
            return await _repo.GetProductByNoAsync(productNo);
        }

        // =====================================================================
        // 共用例外轉譯器
        // =====================================================================
        private BusinessRuleException TranslateSqlException(SqlException sqlex)
        {
            string friendlyMsg = $"資料庫寫入異常(代碼：{sqlex.Number})，請聯絡系統管理員進行查修。";

            if (sqlex.Number == 2627 || sqlex.Number == 2601)
            {
                friendlyMsg = "系統拒絕存檔：商品編號不可與現有資料重複！";
            }
            if (sqlex.Number == 547)
            {
                // 💡 針對商品檔特有的 CHECK 約束進行精準翻譯
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