using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Film;
using api.Helpers;
using api.Interfaces;
using api.Model;
using Data;
using Microsoft.EntityFrameworkCore;

namespace api.Repository
{
    public class FilmRepository : IFilmRepository
    {
        private readonly ApplicationDBContext _context;

        public FilmRepository(ApplicationDBContext context)
        {
            _context = context;
        }

        public async Task<List<Films>> GetAllAsync(QueryObject query)
        {
            var films = _context.Films.Include(c => c.Comments).ThenInclude(a => a.AppUser).AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Name))
            {
                films = films.Where(s => s.Name.ToLower().Contains(query.Name.ToLower()));
            }

            if (!string.IsNullOrWhiteSpace(query.Genre))
            {
                films = films.Where(s => s.Genre.ToLower().Contains(query.Genre.ToLower()));
            }

            if (query.MinYear.HasValue)
            {
                films = films.Where(s => s.ReleaseYear >= query.MinYear.Value);
            }

            if (query.MaxYear.HasValue)
            {
                films = films.Where(s => s.ReleaseYear <= query.MaxYear.Value);
            }

            if (query.SortBy.HasValue)
            {
                switch (query.SortBy.Value)
                {
                    case SortByOptions.Name:
                        films = query.IsDescending ? films.OrderByDescending(s => s.Name) : films.OrderBy(s => s.Name);
                        break;
                    case SortByOptions.Genre:
                        films = query.IsDescending ? films.OrderByDescending(s => s.Genre) : films.OrderBy(s => s.Genre);
                        break;
                    case SortByOptions.ReleaseYear:
                        films = query.IsDescending ? films.OrderByDescending(s => s.ReleaseYear) : films.OrderBy(s => s.ReleaseYear);
                        break;
                }
            }

            var skipNumber = (query.PageNumber - 1) * query.PageSize;
            return await films.Skip(skipNumber).Take(query.PageSize).ToListAsync();
        }

        public async Task<Films?> GetByIdAsync(int id)
        {
            return await _context.Films.Include(c => c.Comments).FirstOrDefaultAsync(i => i.Id == id);
        }

        public Task<List<Films>> GetByNameAsync(string name)
        {
            var lowerName = name.ToLower();
            return _context.Films
                .Include(c => c.Comments)
                .Where(f => f.Name.ToLower().Contains(lowerName))
                .ToListAsync();
        }

        public async Task<Films> CreateAsync(Films filmModel)
        {
            await _context.Films.AddAsync(filmModel);
            await _context.SaveChangesAsync();
            return filmModel;
        }

        public async Task<Films?> UpdateAsync(int id, UpdateFilmRequestDto filmDto)
        {
            var existingFilm = await _context.Films.FirstOrDefaultAsync(x => x.Id == id);
            if (existingFilm == null)
            {
                return null;
            }
            existingFilm.Name = filmDto.Name;
            existingFilm.IMDbRating = filmDto.IMDbRating;
            existingFilm.Description = filmDto.Description;
            existingFilm.Genre = filmDto.Genre;
            existingFilm.Director = filmDto.Director;
            existingFilm.LeadActors = filmDto.LeadActors;
            existingFilm.ReleaseYear = filmDto.ReleaseYear;
            existingFilm.Duration = filmDto.Duration;
            existingFilm.Platform = filmDto.Platform;
            existingFilm.CoverImageUrl = filmDto.CoverImageUrl;
            existingFilm.TrailerUrl = filmDto.TrailerUrl;
            await _context.SaveChangesAsync();
            return existingFilm;
        }

        public async Task<Films?> DeleteAsync(int id)
        {
            var filmModel = await _context.Films.FirstOrDefaultAsync(x => x.Id == id);
            if (filmModel == null)
            {
                return null;
            }
            _context.Films.Remove(filmModel);
            await _context.SaveChangesAsync();
            return filmModel;
        }

        public Task<bool> FilmExists(int id)
        {
            return _context.Films.AnyAsync(s => s.Id == id);
        }

        public Task<bool> FilmExistsByTmdbId(int tmdbId)
        {
            return _context.Films.AnyAsync(s => s.TmdbId == tmdbId);
        }

        public async Task<Films?> GetByNameAsync2(string name)
        {
            return await _context.Films.FirstOrDefaultAsync(s => s.Name == name);
        }

        public async Task<Films?> GetByTmdbIdAsync(int tmdbId)
        {
            return await _context.Films
                .Include(c => c.Comments)
                .ThenInclude(a => a.AppUser)
                .FirstOrDefaultAsync(s => s.TmdbId == tmdbId);
        }

        public async Task<Films> CreateOrUpdateAsync(Films filmModel)
        {
            // TmdbId varsa önce kontrol et
            if (filmModel.TmdbId.HasValue)
            {
                var existing = await GetByTmdbIdAsync(filmModel.TmdbId.Value);
                if (existing != null)
                {
                    // Film zaten var, güncelle
                    existing.Name = filmModel.Name;
                    existing.IMDbRating = filmModel.IMDbRating;
                    existing.Description = filmModel.Description;
                    existing.Genre = filmModel.Genre;
                    existing.Director = filmModel.Director;
                    existing.LeadActors = filmModel.LeadActors;
                    existing.ReleaseYear = filmModel.ReleaseYear;
                    existing.Duration = filmModel.Duration;
                    existing.Platform = filmModel.Platform;
                    existing.CoverImageUrl = filmModel.CoverImageUrl;
                    existing.TrailerUrl = filmModel.TrailerUrl;
                    await _context.SaveChangesAsync();
                    return existing;
                }
            }
            
            // Film yok, yeni oluştur
            await _context.Films.AddAsync(filmModel);
            await _context.SaveChangesAsync();
            return filmModel;
        }
    }
}
