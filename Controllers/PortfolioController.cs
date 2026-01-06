using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Film;
using api.Extensions;
using api.Interfaces;
using api.Model;
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
        public PortfolioController(UserManager<AppUser> userManager,
        IFilmRepository filmRepo, IPortfolioRepository portfolioRepo)
        {
            _userManager = userManager;
            _filmRepo = filmRepo;
            _portfolioRepo = portfolioRepo;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetUserPortfolio()
        {
            var username = User.GetUsername();
            var AppUser = await _userManager.FindByNameAsync(username);
            var userPortfolio = await _portfolioRepo.GetUserPortfolio(AppUser);
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
            
            return CreatedAtAction(nameof(GetUserPortfolio), new { filmId = film.Id }, film);
        }

        [HttpDelete]
        // [Authorize]

        public async Task<IActionResult> DeletePorfolio(string name){
            var username = User.GetUsername();
            var appUser = await _userManager.FindByNameAsync(username);

            var userPortfolio = await _portfolioRepo.GetUserPortfolio(appUser);

            var filteredFilms = userPortfolio.Where(s => s.Name.ToLower() == name.ToLower()).ToList();

            if(filteredFilms.Count()==1){
                await _portfolioRepo.DeletePorfolio(appUser,name);
                return StatusCode(200,"Successfully deleted from portfolio");
            }else{
                return BadRequest("Film not in your portfolio");
            }
            return Ok();
        }
    }
}
