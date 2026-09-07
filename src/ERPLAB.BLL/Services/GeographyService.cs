using ERPLAB.DataAccess.Repositories;
using ERPLAB.Models.Entities;

namespace ERPLAB.BLL.Services
{
    /// <summary>
    /// 地理基礎資料商業邏輯服務 (BLL)
    /// 負責處理縣市與行政區等共用基礎資料之讀取請求。
    /// 核心職責：維持嚴格分層架構 (Strict Layered Architecture) 之絕對一致性，
    /// 提供展示層 (UI) 存取底層字典檔之唯一合法通道，確保 UI 與資料存取層 (DAL) 達成 100% 物理隔離。
    /// </summary>
    public class GeographyService
    {
        private readonly GeographyRepository _repo;

        public GeographyService()
        {
            _repo = new GeographyRepository();
        }

        /// <summary>
        /// 取得所有啟用中之縣市清單
        /// </summary>
        /// <returns>縣市實體集合</returns>
        public async Task<List<Base_City>> GetActiveCitiesAsync()
        {
            return await _repo.GetActiveCitiesAsync();
        }

        /// <summary>
        /// 取得所有啟用中之行政區清單
        /// </summary>
        /// <returns>行政區實體集合</returns>
        public async Task<List<Base_District>> GetAllActiveDistrictsAsync()
        {
            return await _repo.GetAllActiveDistrictsAsync();
        }
    }
}