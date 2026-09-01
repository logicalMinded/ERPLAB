using ERPLAB.Models.Entities;
using System.Data;

namespace ERPLAB.DataAccess.Core
{
    /// <summary>
    /// 表值參數 (TVP) 轉換引擎
    /// </summary>
    public static class TvpHelper
    {
        /// <summary>
        /// 💡 將 C# 實體集合轉化為 SQL 認可的 DataTable。
        /// 必須與資料庫 [dbo].[SalesDetailType] [PurchaseDetailType] [InventoryDetailType]的欄位順序與型別 100% 絕對吻合。
        /// </summary>
        /// 
        public static DataTable CreateSalesDetailTvp(IEnumerable<SalesDetail> details)
        {
            DataTable table = new DataTable();
            table.Columns.Add("LineNo", typeof(int));
            table.Columns.Add("ProductID", typeof(int));
            table.Columns.Add("UnitPrice", typeof(decimal));
            table.Columns.Add("Qty", typeof(int));
            table.Columns.Add("Remark", typeof(string));

            if (details != null)
            {
                foreach (var d in details)
                {
                    table.Rows.Add(
                        d.LineNo,
                        d.ProductID,
                        d.UnitPrice,
                        d.Qty,
                        string.IsNullOrWhiteSpace(d.Remark) ? DBNull.Value : (object)d.Remark.Trim());
                }
            }
            return table;
        }

        public static DataTable CreatePurchaseDetailTvp(IEnumerable<PurchaseDetail> details)
        {
            DataTable table = new DataTable();
            table.Columns.Add("LineNo", typeof(int));
            table.Columns.Add("ProductID", typeof(int));
            table.Columns.Add("UnitPrice", typeof(decimal));
            table.Columns.Add("Qty", typeof(int));
            table.Columns.Add("Remark", typeof(string));

            if (details != null)
            {
                foreach (var d in details)
                {
                    table.Rows.Add(d.LineNo, d.ProductID, d.UnitPrice, d.Qty,
                        string.IsNullOrWhiteSpace(d.Remark) ? DBNull.Value : (object)d.Remark.Trim());
                }
            }
            return table;
        }

        public static DataTable CreateInventoryDetailTvp(IEnumerable<InventoryDetail> details)
        {
            DataTable table = new DataTable();
            table.Columns.Add("LineNo", typeof(int));
            table.Columns.Add("ProductID", typeof(int));
            table.Columns.Add("SystemStock", typeof(int));
            table.Columns.Add("ActualStock", typeof(int));
            table.Columns.Add("StockPrice", typeof(decimal));
            table.Columns.Add("Remark", typeof(string));

            if (details != null)
            {
                foreach (var d in details)
                {
                    table.Rows.Add(
                        d.LineNo,
                        d.ProductID,
                        d.SystemStock,
                        d.ActualStock,
                        d.StockPrice,
                        string.IsNullOrWhiteSpace(d.Remark) ? DBNull.Value : (object)d.Remark.Trim());
                }
            }
            return table;
        }
    }
}