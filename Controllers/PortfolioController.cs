using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Film;
using api.Extensions;
using api.Interfaces;
using api.Model;
using api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    [Route("api/portfolio")]
    [ApiController]
    [Authorize]
    public class PortfolioController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IFilmRepository _filmRepo;
        private readonly IPortfolioRepository _portfolioRepo;
        private readonly IRedisCacheService _cache;
        
        public PortfolioController(UserManager<AppUser> userManager,
        IFilmRepository filmRepo, IPortfolioRepository portfolioRepo, IRedisCacheService cache)
        {
            _userManager = userManager;
            _filmRepo = filmRepo;
            _portfolioRepo = portfolioRepo;
            _cache = cache;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetUserPortfolio()
        {
            var username = User.GetUsername();
            var appUser = await _userManager.FindByNameAsync(username);
            
            // Cache key per user
            var cacheKey = $"portfolio_{appUser.Id}";
            var cachedPortfolio = await _cache.GetCacheValueAsync<IEnumerable<Films>>(cacheKey);
            
            if (cachedPortfolio != null)
            {
                Console.WriteLine($"💾 Returning portfolio from cache for user {username}");
                return Ok(cachedPortfolio);
            }
            
            Console.WriteLine($"🔄 Fetching portfolio from database for user {username}");
            var userPortfolio = await _portfolioRepo.GetUserPortfolio(appUser);
            
            // Cache for 10 minutes (user-specific data changes frequently)
            await _cache.SetCacheValueAsync(cacheKey, userPortfolio, TimeSpan.FromMinutes(10));
            
            return Ok(userPortfolio);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddPortfolio([FromBody] AddFilmToPortfolioDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var username = User.GetUsername();
            var appUser = await _userManager.FindByNameAsync(username);
            
            Films film;

            // TmdbId varsa önce ona göre kontrol et
            if (dto.TmdbId.HasValue)
            {
                film = await _filmRepo.GetByTmdbIdAsync(dto.TmdbId.Value);
                
                // Film yoksa oluştur
                if (film == null)
                {
                    film = new Films
                    {
                        TmdbId = dto.TmdbId,
                        Name = dto.Name,
                        IMDbRating = dto.IMDbRating,
                        Description = dto.Description,
                        Genre = dto.Genre,
                        Director = dto.Director,
                        LeadActors = dto.LeadActors,
                        ReleaseYear = dto.ReleaseYear,
                        Duration = dto.Duration,
                        Platform = dto.Platform,
                        CoverImageUrl = dto.CoverImageUrl,
                        TrailerUrl = dto.TrailerUrl
                    };
                    
                    film = await _filmRepo.CreateAsync(film);
                }
            }
            else
            {
                // TmdbId yoksa isme göre ara
                film = await _filmRepo.GetByNameAsync2(dto.Name);
                
                if (film == null)
                    return BadRequest("Film not found and no TmdbId provided");
            }

            // Portfolyoda zaten var mı kontrol et
            var userPortfolio = await _portfolioRepo.GetUserPortfolio(appUser);
            if (userPortfolio.Any(e => e.Id == film.Id))
                return BadRequest("Film already in portfolio");

            var portfolioModel = new Portfolio
            {
                FilmId = film.Id,
                AppUserId = appUser.Id
            };

            await _portfolioRepo.CreateAsync(portfolioModel);
            
            // Clear user's portfolio cache
            await _cache.RemoveCacheValueAsync($"portfolio_{appUser.Id}");
            Console.WriteLine($"🗑️ Portfolio cache cleared for user {username}");
            
            return CreatedAtAction(nameof(GetUserPortfolio), new { filmId = film.Id }, film);
        }

        [HttpDelete]
        public async Task<IActionResult> DeletePorfolio(string name){
            var username = User.GetUsername();
            var appUser = await _userManager.FindByNameAsync(username);

            var userPortfolio = await _portfolioRepo.GetUserPortfolio(appUser);

            var filteredFilms = userPortfolio.Where(s => s.Name.ToLower() == name.ToLower()).ToList();

            if(filteredFilms.Count()==1){
                await _portfolioRepo.DeletePorfolio(appUser,name);
                
                // Clear user's portfolio cache
                await _cache.RemoveCacheValueAsync($"portfolio_{appUser.Id}");
                Console.WriteLine($"🗑️ Portfolio cache cleared after deletion for user {username}");
                
                return StatusCode(200,"Successfully deleted from portfolio");
            }else{
                return BadRequest("Film not in your portfolio");
            }
        }
    }
}
