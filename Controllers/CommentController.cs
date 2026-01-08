using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Comment;
using api.Extensions;
using api.Interfaces;
using api.Mappers;
using api.Model;
using api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;


namespace api.Controllers
{
    [Route("api/comment")]
    [ApiController]
    [Authorize]
    public class CommentController : ControllerBase
    {
        private readonly ICommentRepository _commentRepo;
        private readonly IFilmRepository _filmRepo;
        private readonly UserManager<AppUser> _userManager;
        private readonly IProfanityFilterService _profanityFilter;

        public CommentController(ICommentRepository commentRepo, IFilmRepository filmRepo, UserManager<AppUser> userManager, IProfanityFilterService profanityFilter)
        {
            _commentRepo = commentRepo;
            _filmRepo = filmRepo;
            _userManager = userManager;
            _profanityFilter = profanityFilter;
        }

        [HttpGet]
        public async Task<ActionResult> GetAll()
        {
if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var comments = await _commentRepo.GetAllAsync();
            var CommentDto = comments.Select(s => s.ToCommentDto());

            return Ok(CommentDto);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById([FromRoute] int id)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var comments = await _commentRepo.GetByIdAsync(id);
            if (comments == null)
            {
                return NotFound();
            }
            return Ok(comments.ToCommentDto());
        }

        [HttpPost("{filmId:int}")]
        public async Task<IActionResult> Create([FromRoute] int filmId, CreateCommentDto commentDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Küfür kontrolü
            if (_profanityFilter.ContainsProfanity(commentDto.Content))
            {
                return BadRequest(new { message = "Your comment contains inappropriate language. Please revise your comment.", 
                                       messagetr = "Yorumunuz uygunsuz içerik barındırıyor. Lütfen yorumunuzu düzenleyin." });
            }

            if (!await _filmRepo.FilmExists(filmId))
            {
                return BadRequest("Film does not exist");
            }

            var username = User.GetUsername();
            var appUser = await _userManager.FindByNameAsync(username);

            var commentModel = commentDto.ToCommentFromCreate(filmId);
            commentModel.AppUserId = appUser.Id;
            await _commentRepo.CreateAsync(commentModel);
            return CreatedAtAction(nameof(GetById), new { id = commentModel.Id }, commentModel.ToCommentDto());
        }

        // TMDB'den gelen filmler için yeni endpoint - film yoksa oluşturur
        [HttpPost("with-film")]
        public async Task<IActionResult> CreateWithFilm([FromBody] CreateCommentWithFilmDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Küfür kontrolü
            if (_profanityFilter.ContainsProfanity(dto.Content))
            {
                return BadRequest(new { message = "Your comment contains inappropriate language. Please revise your comment.", 
                                       messagetr = "Yorumunuz uygunsuz içerik barındırıyor. Lütfen yorumunuzu düzenleyin." });
            }

            var username = User.GetUsername();
            var appUser = await _userManager.FindByNameAsync(username);

            Films film;

            // Mevcut FilmId varsa onu kullan
            if (dto.FilmId.HasValue)
            {
                film = await _filmRepo.GetByIdAsync(dto.FilmId.Value);
                if (film == null)
                    return BadRequest("Film not found");
            }
            // TmdbId varsa ona göre bul veya oluştur
            else if (dto.TmdbId.HasValue)
            {
                film = await _filmRepo.GetByTmdbIdAsync(dto.TmdbId.Value);
                
                if (film == null)
                {
                    // Film yoksa oluştur
                    film = new Films
                    {
                        TmdbId = dto.TmdbId,
                        Name = dto.FilmName ?? "Unknown",
                        IMDbRating = dto.IMDbRating ?? 0,
                        Description = dto.Description ?? string.Empty,
                        Genre = dto.Genre ?? string.Empty,
                        Director = dto.Director ?? string.Empty,
                        LeadActors = dto.LeadActors ?? string.Empty,
                        ReleaseYear = dto.ReleaseYear ?? DateTime.Now.Year,
                        Duration = dto.Duration ?? 0,
                        Platform = dto.Platform ?? "Unknown",
                        CoverImageUrl = dto.CoverImageUrl,
                        TrailerUrl = dto.TrailerUrl
                    };
                    
                    film = await _filmRepo.CreateAsync(film);
                }
            }
            else
            {
                return BadRequest("Either FilmId or TmdbId must be provided");
            }

            // Yorumu oluştur
            var commentModel = new Comment
            {
                StarRating = dto.StarRating,
                Content = dto.Content,
                ContainsSpoiler = dto.ContainsSpoiler,
                FilmId = film.Id,
                AppUserId = appUser.Id
            };

            await _commentRepo.CreateAsync(commentModel);
            return CreatedAtAction(nameof(GetById), new { id = commentModel.Id }, commentModel.ToCommentDto());
        }

        [HttpDelete]
        [Route("{id:int}")]
        public async Task<IActionResult> Delete([FromRoute] int id)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var commentModel = await _commentRepo.DeleteAsync(id);
            if (commentModel == null)
            {
                return NotFound("Comment does not exist");
            }
            
            return Ok(commentModel);
        }

        [HttpPut]
        [Route("{id:int}")]
        public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateCommentDto updateDto)
        {
if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var comment = await _commentRepo.UpdateAsync(id, updateDto.ToCommentFromUpdate());
            if (comment == null)
            {
                return NotFound("Comment not found");
            }
            return Ok(comment.ToCommentDto());
        }

    }


}

