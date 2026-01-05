using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Comment;
using api.Extensions;
using api.Interfaces;
using api.Mappers;
using api.Model;
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

        public CommentController(ICommentRepository commentRepo, IFilmRepository filmRepo, UserManager<AppUser> userManager)
        {
            _commentRepo = commentRepo;
            _filmRepo = filmRepo;
            _userManager = userManager;
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

