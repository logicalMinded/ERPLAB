using ERPLAB.DataAccess.Core;
using ERPLAB.Models.Entities;
using Microsoft.Data.SqlClient;
namespace ERPLAB.DataAccess.Repositories
{
    /// <summary>
    /// 地理基礎資料倉儲 (Repository)
    /// 負責提供縣市與行政區等靜態參照資料，主要供前端介面 (UI) 下拉選單進行資料綁定與層級連動。
    /// </summary>
    public class GeographyRepository
    {
        /// <summary>
        /// 取得所有啟用中的縣市資料，並依自訂權重 (SortSeq) 排序。
        /// 配合資料庫層級的覆蓋索引 (Covering Index) 設計，最佳化基礎資料的查詢讀取效能。
        /// </summary>
        public async Task<List<Base_City>> GetActiveCitiesAsync()
        {
            var list = new List<Base_City>();

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
        /// 取得所有啟用中的行政區資料，並依自訂權重排序。
        /// 架構考量：由於全台行政區數量固定且異動頻率極低，採一次性載入至應用程式記憶體，
        /// 後續 UI 切換縣市時直接透過 LINQ 進行篩選，以大幅減少頻繁的資料庫 I/O 往返成本。
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