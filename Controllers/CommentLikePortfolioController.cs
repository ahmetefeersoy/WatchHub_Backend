using api.Extensions;
using api.Interfaces;
using api.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
namespace api.Controllers
{
    [Route("api/CommentLikePortfolio")]
    [ApiController]
    [Authorize]
    public class CommentLikePortfolioController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ICommentRepository _commentRepo;
        private readonly ICommentLikePortfolioRepository _commentLikePortfolioRepo;
        public CommentLikePortfolioController(UserManager<AppUser> userManager,
        ICommentRepository commentRepo, ICommentLikePortfolioRepository commentLikePortfolioRepo)
        {
            _userManager = userManager;
            _commentRepo = commentRepo;
            _commentLikePortfolioRepo = commentLikePortfolioRepo;
        }
        [HttpGet]
        [Authorize]

        public async Task<IActionResult> GetUserCommentLikePortfolio()
        {
            var username = User.GetUsername();
            var AppUser = await _userManager.FindByNameAsync(username);
            var userCommentLikePortfolio = await _commentLikePortfolioRepo.GetUserCommentLikePortfolio(AppUser);
            return Ok(userCommentLikePortfolio);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddToCommandLikePortfolio(int IdOfCommand)
        {

            var username = User.GetUsername();
            var AppUser = await _userManager.FindByNameAsync(username);
            var comment = await _commentRepo.GetByIdAsync(IdOfCommand);

            if (comment == null) return BadRequest("Comment not found");

            var userCommentLikePortfolio = await _commentLikePortfolioRepo.GetUserCommentLikePortfolio(AppUser);

            if (userCommentLikePortfolio.Any(e => e.Id == IdOfCommand)) return BadRequest("You cannot like same comment");

            var commentLikePortfolioModel = new CommentLikePortfolio
            {
                CommentId = comment.Id,
                AppUserId = AppUser.Id
            };
            await _commentLikePortfolioRepo.LikeCommendAsync(commentLikePortfolioModel);
            if (commentLikePortfolioModel == null)
            {
                return StatusCode(500, "Could not like");
            }
            else
            {
                await _commentLikePortfolioRepo.UpdateLikesAsync(IdOfCommand);
                return StatusCode(200, "You liked the comment.");
            }
        }

        [HttpDelete]
        [Authorize]

        public async Task<IActionResult> RemoveFromCommendLikePorfolio(int IdOfCommand)
        {
            var username = User.GetUsername();
            var appUser = await _userManager.FindByNameAsync(username);

            var userCommentLikePortfolio = await _commentLikePortfolioRepo.GetUserCommentLikePortfolio(appUser);

            var filteredLikeComments = userCommentLikePortfolio.Where(s => s.Id == IdOfCommand).ToList();

            if (filteredLikeComments.Count() == 1)
            {
                await _commentLikePortfolioRepo.DislikeCommendAsync(appUser, IdOfCommand);
                await _commentLikePortfolioRepo.UpdateDislikesAsync(IdOfCommand);
                return StatusCode(200, "Successfully disliked");
            }
            else
            {
                return BadRequest("First you should like the comment.");
            }
            return Ok();
        }


    }
}