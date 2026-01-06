using System.Collections.Generic;
using System.Threading.Tasks;
using api.Dtos.Film;
using api.Helpers;
using api.Model;

namespace api.Interfaces
{
    public interface IFilmRepository
    {
        Task<List<Films>> GetAllAsync(QueryObject query);
        Task<Films?> GetByIdAsync(int id);
        Task<Films?> GetByTmdbIdAsync(int tmdbId);
        Task<Films?> GetByNameAsync2(string name);
        Task<List<Films>> GetByNameAsync(string name);
        Task<Films> CreateAsync(Films filmModel);
        Task<Films> CreateOrUpdateAsync(Films filmModel);
        Task<Films?> UpdateAsync(int id , UpdateFilmRequestDto filmDto);
        Task<Films?> DeleteAsync(int id);
        Task<bool> FilmExists(int id);
        Task<bool> FilmExistsByTmdbId(int tmdbId);
    }
}