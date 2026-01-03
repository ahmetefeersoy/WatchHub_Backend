using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Film;
using api.Helpers;
using api.Interfaces;
using api.Mappers;
using api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    [Route("api/films")]
    [ApiController]
    public class FilmController : ControllerBase
    {
        private readonly IFilmRepository _filmRepo;
        private readonly IRedisCacheService _cache;

        public FilmController(IFilmRepository filmRepo, IRedisCacheService cache)
        {
            _filmRepo = filmRepo;
            _cache = cache;
        }

        [HttpGet]
        [Authorize]
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
            
            // Store in cache for 1 hour
            await _cache.SetCacheValueAsync(cacheKey, filmDto, TimeSpan.FromHours(1));
            
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
public async Task<IActionResult> Create([FromBody] CreateFilmRequestDto filmDto)
{   
    if (!ModelState.IsValid)
        return BadRequest(ModelState);

    var filmModel = filmDto.ToFilmFromCreateDto();
    await _filmRepo.CreateAsync(filmModel);

    // Düzeltme: Film oluşturulmadan önce filmModel.Id ayarlanmalıdır.
    return CreatedAtAction(nameof(GetById), new { id = filmModel.Id }, filmModel.ToFilmDto());
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
    }
}