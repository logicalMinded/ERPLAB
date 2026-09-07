using ERPLAB.Models.Entities;
using System.Data;
namespace ERPLAB.DataAccess.Core
{
    /// <summary>
    /// 表值參數 (Table-Valued Parameter, TVP) 轉換輔助類別 (TvpHelper)。
    /// 負責將領域實體集合轉換為 SQL Server TVP 所需的 DataTable 格式，
    /// 透過單次資料庫往返 (Round-trip) 即可完成主檔與明細檔的批次寫入，有效降低 I/O 成本並提升交易效能。
    /// </summary>
    public static class TvpHelper
    {
        /// <summary>
        /// 將銷貨明細集合轉換為 DataTable。
        /// 注意：欄位定義與順序必須與資料庫自訂表型別 (UDTT) [dbo].[SalesDetailType] 嚴格一致，否則將引發底層對應錯誤。
        /// </summary>
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

        /// <summary>
        /// 將進貨明細集合轉換為 DataTable。
        /// 注意：欄位定義與順序必須與資料庫自訂表型別 (UDTT) [dbo].[PurchaseDetailType] 嚴格一致。
        /// </summary>
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

        /// <summary>
        /// 將盤點明細集合轉換為 DataTable。
        /// 注意：欄位定義與順序必須與資料庫自訂表型別 (UDTT) [dbo].[InventoryDetailType] 嚴格一致。
        /// </summary>
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