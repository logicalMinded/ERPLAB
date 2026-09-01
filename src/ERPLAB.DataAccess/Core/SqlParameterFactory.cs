using Microsoft.Data.SqlClient;
using System.Data;

namespace ERPLAB.DataAccess.Core
{
    public static class SqlParameterFactory
    {
        /// <summary>
        /// 專門處理 VARCHAR 型別 (如 Username, TaxID)，阻斷 C# string 預設轉 NVarChar 的效能地雷
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
        /// 處理 NVARCHAR 型別 (如 NodeName, Remark)
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
        /// 處理 VARBINARY 型別 (如 舊版本的PasswordHash, Salt)
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
        /// 處理樂觀鎖專用 TIMESTAMP (RowVersion) 型別
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
        /// 處理高精確度財務數值，精準對齊 DECIMAL(18,2)
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
        /// 處理 BIT 型別 (如 IsActive, IsLocked)，並自動處理 bool? 空值轉換
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