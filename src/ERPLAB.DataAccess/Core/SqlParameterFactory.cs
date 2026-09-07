using Microsoft.Data.SqlClient;
using System.Data;
namespace ERPLAB.DataAccess.Core
{
    /// <summary>
    /// SQL 參數工廠 (SqlParameterFactory)
    /// 集中管理並建立強型別 SqlParameter。明確指定 SqlDbType 以避免 .NET 預設型別對應 (如 string 預設為 NVARCHAR)
    /// 所引發的資料庫隱式轉型 (Implicit Conversion)，確保執行計畫效能與索引有效性。
    /// </summary>
    public static class SqlParameterFactory
    {
        /// <summary>
        /// 建立 VARCHAR 型別參數。
        /// 適用於非 Unicode 字串 (如帳號、統一編號)，避免隱式轉型導致索引失效 (Index Scan)。
        /// </summary>
        public static SqlParameter CreateVarChar(string name, string? value, int size = -1)
        {
            return new SqlParameter
            {
                ParameterName = name,
                SqlDbType = SqlDbType.VarChar,
                Size = size,
                Value = (object?)value ?? DBNull.Value
            };
        }

        /// <summary>
        /// 建立 NVARCHAR 型別參數。
        /// 適用於需支援多國語言之 Unicode 字串欄位。
        /// </summary>
        public static SqlParameter CreateNVarChar(string name, string? value, int size = -1)
        {
            return new SqlParameter
            {
                ParameterName = name,
                SqlDbType = SqlDbType.NVarChar,
                Size = size,
                Value = (object?)value ?? DBNull.Value
            };
        }

        /// <summary>
        /// 建立 VARBINARY 型別參數。
        /// 適用於二進位資料儲存 (如密碼雜湊值、二進位檔案)。
        /// </summary>
        public static SqlParameter CreateVarBinary(string name, byte[]? value, int size = -1)
        {
            return new SqlParameter
            {
                ParameterName = name,
                SqlDbType = SqlDbType.VarBinary,
                Size = size,
                Value = value != null && value.Length > 0 ? value : DBNull.Value
            };
        }

        /// <summary>
        /// 建立 TIMESTAMP (RowVersion) 型別參數。
        /// 專供資料庫樂觀鎖 (Optimistic Concurrency) 併發控制機制使用。
        /// </summary>
        public static SqlParameter CreateTimestamp(string name, byte[] value)
        {
            return new SqlParameter
            {
                ParameterName = name,
                SqlDbType = SqlDbType.Timestamp,
                Value = value
            };
        }

        /// <summary>
        /// 建立 INT 型別參數，並封裝 Nullable 型別至 DBNull 的轉換邏輯。
        /// </summary>
        public static SqlParameter CreateInt(string name, int? value)
        {
            return new SqlParameter
            {
                ParameterName = name,
                SqlDbType = SqlDbType.Int,
                Value = (object?)value ?? DBNull.Value
            };
        }

        public static SqlParameter CreateTinyInt(string name, byte value)
        {
            return new SqlParameter
            {
                ParameterName = name,
                SqlDbType = SqlDbType.TinyInt,
                Value = value
            };
        }

        /// <summary>
        /// 建立 DECIMAL 型別參數。
        /// 預設精度為 DECIMAL(18,2)，適用於需要高精確度運算之財務或金額欄位。
        /// </summary>
        public static SqlParameter CreateDecimal(string name, decimal value, byte precision = 18, byte scale = 2)
        {
            return new SqlParameter
            {
                ParameterName = name,
                SqlDbType = SqlDbType.Decimal,
                Precision = precision,
                Scale = scale,
                Value = value
            };
        }

        /// <summary>
        /// 建立 BIT 型別參數，並支援 Nullable 布林值的空值轉換。
        /// </summary>
        public static SqlParameter CreateBit(string name, bool? value)
        {
            return new SqlParameter
            {
                ParameterName = name,
                SqlDbType = SqlDbType.Bit,
                Value = (object?)value ?? DBNull.Value
            };
        }
    }
}