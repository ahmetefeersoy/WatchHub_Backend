using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Film;
using api.Helpers;
using api.Interfaces;
using api.Mappers;
using api.Model;
using api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    [Route("api/films")]
    [ApiController]
    [Authorize]
    public class FilmController : ControllerBase
    {
        private readonly IFilmRepository _filmRepo;
        private readonly IRedisCacheService _cache;
        private readonly ITmdbService _tmdbService;

        public FilmController(IFilmRepository filmRepo, IRedisCacheService cache, ITmdbService tmdbService)
        {
            _filmRepo = filmRepo;
            _cache = cache;
            _tmdbService = tmdbService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] QueryObject query)
        {   
                if(!ModelState.IsValid)
                return BadRequest(ModelState);

            // Redis cache key based on query parameters
            var cacheKey = $"films_list_{query.SortBy}_{query.IsDescending}_{query.PageNumber}_{query.PageSize}";
            
            // Try to get from cache
            var cachedFilms = await _cache.GetCacheValueAsync<List<object>>(cacheKey);
            if (cachedFilms != null)
            {
                return Ok(cachedFilms);
            }

            // If not in cache, get from database
            var films = await _filmRepo.GetAllAsync(query);
            var filmDto = films.Select(s => s.ToFilmDto()).ToList();
            
            // Store in cache for 6 hours (recommended for popular/trending lists)
            await _cache.SetCacheValueAsync(cacheKey, filmDto, TimeSpan.FromHours(6));
            
            return Ok(filmDto);
        }

        [HttpGet("search/{name}")]
        public async Task<IActionResult> GetByName([FromRoute]string name)
        {       if(!ModelState.IsValid)
                return BadRequest(ModelState);

            var films = await _filmRepo.GetByNameAsync(name);
            if (!films.Any())
            {
                return NotFound();
            }
            var filmDto = films.Select(s => s.ToFilmDto());
            return Ok(filmDto);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById([FromRoute]int id)
        {       if(!ModelState.IsValid)
                return BadRequest(ModelState);

            // Try to get from cache
            var cacheKey = $"film_{id}";
            var cachedFilm = await _cache.GetCacheValueAsync<object>(cacheKey);
            if (cachedFilm != null)
            {
                return Ok(cachedFilm);
            }

            // If not in cache, get from database
            var film = await _filmRepo.GetByIdAsync(id);
            if (film == null)
            {
                return NotFound();
            }
            
            var filmDto = film.ToFilmDto();
            
            // Store in cache for 24 hours
            await _cache.SetCacheValueAsync(cacheKey, filmDto, TimeSpan.FromHours(24));
            
            return Ok(filmDto);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Create([FromBody] CreateFilmRequestDto filmDto)
        {   
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try 
            {
                var filmModel = filmDto.ToFilmFromCreateDto();
                await _filmRepo.CreateAsync(filmModel);

                return CreatedAtAction(nameof(GetById), new { id = filmModel.Id }, filmModel.ToFilmDto());
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = "Failed to create film", Details = ex.Message, Inner = ex.InnerException?.Message });
            }
        }


       [HttpPut("{id:int}")]
public async Task<IActionResult> Update([FromRoute]int id, [FromBody] UpdateFilmRequestDto updateDto)
{   
    if (!ModelState.IsValid)
        return BadRequest(ModelState);

    var filmModel = await _filmRepo.UpdateAsync(id, updateDto);
    if (filmModel == null)
    {
        return NotFound();
    }
    
    return Ok(filmModel.ToFilmDto()); // Düzeltme: ToFilmDto metodunu kullanın
}


        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete([FromRoute] int id)
        {   
                if(!ModelState.IsValid)
                return BadRequest(ModelState);
                
            var filmModel = await _filmRepo.DeleteAsync(id);
            if (filmModel == null)
            {
                return NotFound();
            }
            return NoContent();
        }

        [HttpPost("import-from-tmdb/preview")]
        [AllowAnonymous]
        public async Task<IActionResult> PreviewImportFromTmdb([FromBody] ImportFilmRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // TMDB API requires page >= 1
            if (request.Page < 1) request.Page = 1;
            if (request.Limit < 1) request.Limit = 5;

            try
            {
                // Check cache first (6 hour expiry for TMDB data)
                // Include genreId in cache key for genre-specific caching
                var cacheKey = request.GenreId.HasValue
                    ? $"tmdb_genre_{request.GenreId}_p{request.Page}_l{request.Limit}"
                    : $"tmdb_popular_p{request.Page}_l{request.Limit}";
                    
                var cachedData = await _cache.GetCacheValueAsync<List<FilmDto>>(cacheKey);
                
                if (cachedData != null)
                {
                    return Ok(new 
                    { 
                        Message = "Preview from cache",
                        Count = cachedData.Count,
                        Films = cachedData,
                        Cached = true
                    });
                }

                // Fetch from TMDB if not cached (with optional genre filter)
                var movies = await _tmdbService.FetchPopularMoviesAsync(request.Page, request.Limit, request.GenreId);
                var filmDtos = movies.Select(m => m.ToFilmDto()).ToList();
                
                // Cache for 6 hours
                await _cache.SetCacheValueAsync(cacheKey, filmDtos, TimeSpan.FromHours(6));
                
                return Ok(new 
                { 
                    Message = "Preview from TMDB",
                    Count = filmDtos.Count,
                    Films = filmDtos,
                    Cached = false
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpPost("import-from-tmdb")]
        public async Task<IActionResult> ImportFromTmdb([FromBody] ImportFilmRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // TMDB API requires page >= 1
            if (request.Page < 1) request.Page = 1;
            if (request.Limit < 1) request.Limit = 5;

            try
            {
                var movies = await _tmdbService.FetchPopularMoviesAsync(request.Page, request.Limit, request.GenreId);
                var savedFilms = new List<Films>();
                var updatedFilms = new List<Films>();

                foreach (var movie in movies)
                {
                    // TmdbId ile kontrol et ve gerekirse güncelle
                    var existingFilm = movie.TmdbId.HasValue 
                        ? await _filmRepo.GetByTmdbIdAsync(movie.TmdbId.Value) 
                        : null;

                    if (existingFilm != null)
                    {
                        // Film zaten var, güncelle
                        var updated = await _filmRepo.CreateOrUpdateAsync(movie);
                        updatedFilms.Add(updated);
                    }
                    else
                    {
                        // Yeni film oluştur
                        var savedFilm = await _filmRepo.CreateAsync(movie);
                        savedFilms.Add(savedFilm);
                    }
                }

                // Invalidate cache after import
                await _cache.RemoveCacheValueAsync("films_list_*");

                return Ok(new 
                { 
                    Message = $"Import completed: {savedFilms.Count} new, {updatedFilms.Count} updated",
                    NewFilms = savedFilms.Count,
                    UpdatedFilms = updatedFilms.Count,
                    Films = savedFilms.Select(f => f.ToFilmDto()).ToList()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpPost("search-tmdb")]
        [AllowAnonymous]
        public async Task<IActionResult> SearchTmdb([FromBody] SearchTmdbRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (string.IsNullOrWhiteSpace(request.Query))
                return BadRequest("Search query cannot be empty");

            try
            {
                // Cache key for search results
                var cacheKey = $"tmdb_search_{request.Query.ToLower().Replace(" ", "_")}_p{request.Page}";
                
                // Try cache first
                var cachedResults = await _cache.GetCacheValueAsync<List<FilmDto>>(cacheKey);
                if (cachedResults != null)
                {
                    return Ok(new 
                    { 
                        Message = "Search results from cache",
                        Query = request.Query,
                        Count = cachedResults.Count,
                        Films = cachedResults,
                        Cached = true
                    });
                }

                // Search TMDB
                var movies = await _tmdbService.SearchMoviesAsync(request.Query, request.Page, request.Limit);
                var filmDtos = movies.Select(m => m.ToFilmDto()).ToList();
                
                // Cache for 1 hour (search results change less frequently)
                await _cache.SetCacheValueAsync(cacheKey, filmDtos, TimeSpan.FromHours(1));
                
                return Ok(new 
                { 
                    Message = "Search results from TMDB",
                    Query = request.Query,
                    Count = filmDtos.Count,
                    Films = filmDtos,
                    Cached = false
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }
    }
}