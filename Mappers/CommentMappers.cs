using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Comment;
using api.Model;

namespace api.Mappers
{
    public static class CommentMappers
    {
        public static CommentDto ToCommentDto(this Comment commentModel){

            return new CommentDto{
                Id = commentModel.Id,
                StarRating = commentModel.StarRating,
                NumberOfLikes = commentModel.NumberOfLikes,
                Content = commentModel.Content ?? string.Empty, // Null kontrolü
                ContainsSpoiler = commentModel.ContainsSpoiler,
                CreatedOn = commentModel.CreatedOn,
                CreatedBy = commentModel.AppUser?.UserName ?? "Unknown", // Null kontrolü
                FilmId = commentModel.FilmId

            };
        }
           public static Comment ToCommentFromCreate(this CreateCommentDto commentDto, int filmId){

            return new Comment{
                StarRating = commentDto.StarRating,
                Content = commentDto.Content,
                ContainsSpoiler = commentDto.ContainsSpoiler,
                FilmId = filmId

            };
        }

         public static Comment ToCommentFromUpdate(this UpdateCommentDto commentDto){

            return new Comment{
                StarRating = commentDto.StarRating,
                Content = commentDto.Content,

            };
        }
    }
}