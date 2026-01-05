using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        public async Task<IActionResult> AddPortfolio([FromBody]string name){

            var username = User.GetUsername();
            var AppUser = await _userManager.FindByNameAsync(username);
            var film = await _filmRepo.GetByNameAsync2(name);
            
            if(film==null) return BadRequest("Film not found");

            var userPortfolio = await _portfolioRepo.GetUserPortfolio(AppUser);

            if(userPortfolio.Any(e => e.Name.ToLower()== name.ToLower())) return BadRequest("Cannot add same film to portfolio");

            var portfolioModel = new Portfolio
            {
                FilmId = film.Id,
                AppUserId = AppUser.Id
            };
            await _portfolioRepo.CreateAsync(portfolioModel);
            if(portfolioModel ==null){
                return StatusCode(500,"Could not create");
            }else{
                return Created();
            }
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
