using ERPLAB.DataAccess.Repositories;
using ERPLAB.Models.Entities;

namespace ERPLAB.BLL.Services
{
    /// <summary>
    /// 地理基礎資料服務 (BLL 大腦)
    /// 核心職責：隔離 UI 與 DAL，未來若需加入快取 (Redis) 或部門權限過濾，皆於此處攔截。
    /// </summary>
    public class GeographyService
    {
        private readonly GeographyRepository _repo;

        public GeographyService()
        {
            _repo = new GeographyRepository();
        }

        public async Task<List<Base_City>> GetActiveCitiesAsync()
        {
            return await _repo.GetActiveCitiesAsync();
        }

        public async Task<List<Base_District>> GetAllActiveDistrictsAsync()
        {
            return await _repo.GetAllActiveDistrictsAsync();
        }
    }
}