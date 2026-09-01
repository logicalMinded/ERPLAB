using ERPLAB.DataAccess.Core;
using ERPLAB.Models.Entities;
using Microsoft.Data.SqlClient;

namespace ERPLAB.DataAccess.Repositories
{
    /// <summary>
    /// 地理基礎資料倉儲。
    /// 核心職責：專供 UI 層下拉選單 (ComboBox) 進行資料綁定與連動。
    /// </summary>
    public class GeographyRepository
    {
        /// <summary>
        /// 取得所有啟用中的縣市，並依據自訂權重 (SortSeq) 排序
        /// </summary>
        public async Task<List<Base_City>> GetActiveCitiesAsync()
        {
            var list = new List<Base_City>();

            // 💡 利用我們先前在 DB 建立的 IX_Base_City_Lookup 覆蓋索引，查詢效能為物理極限
            string sql = "SELECT [CityID], [CityNo], [CityName], [SortSeq] FROM [dbo].[Base_City] WHERE [IsActive] = 1 ORDER BY [SortSeq] ASC, [CityID] ASC;";

            using var conn = await DbConnectionFactory.GetConnectionAsync();
            using var cmd = new SqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(new Base_City
                {
                    CityID = reader.GetInt32(reader.GetOrdinal("CityID")),
                    CityNo = reader.GetString(reader.GetOrdinal("CityNo")),
                    CityName = reader.GetString(reader.GetOrdinal("CityName")),
                    SortSeq = reader.GetInt32(reader.GetOrdinal("SortSeq")),
                    IsActive = true
                });
            }
            return list;
        }

        /// <summary>
        /// 取得所有啟用中的行政區，並依據權重排序
        /// 💡 實務架構決策：一次性撈回全台 368 個行政區存於本機記憶體，
        /// 讓 UI 在切換縣市時直接使用 LINQ 過濾，避免頻繁的資料庫 I/O 往返。
        /// </summary>
        public async Task<List<Base_District>> GetAllActiveDistrictsAsync()
        {
            var list = new List<Base_District>();

            string sql = "SELECT [DistrictID], [CityID], [ZipCode], [DistrictName], [SortSeq] FROM [dbo].[Base_District] WHERE [IsActive] = 1 ORDER BY [CityID] ASC, [SortSeq] ASC;";

            using var conn = await DbConnectionFactory.GetConnectionAsync();
            using var cmd = new SqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(new Base_District
                {
                    DistrictID = reader.GetInt32(reader.GetOrdinal("DistrictID")),
                    CityID = reader.GetInt32(reader.GetOrdinal("CityID")),
                    ZipCode = reader.GetString(reader.GetOrdinal("ZipCode")),
                    DistrictName = reader.GetString(reader.GetOrdinal("DistrictName")),
                    SortSeq = reader.GetInt32(reader.GetOrdinal("SortSeq")),
                    IsActive = true
                });
            }
            return list;
        }
    }
}