using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Film;
using api.Model;

namespace api.Mappers
{
    public static class FilmMappers
    {
        public static FilmDto ToFilmDto(this Films filmModel)
        {
            return new FilmDto
            {
                Id = filmModel.Id,
                Name = filmModel.Name,
                IMDbRating = filmModel.IMDbRating,
                Description = filmModel.Description,
                Genre = filmModel.Genre,
                Director = filmModel.Director,
                LeadActors = filmModel.LeadActors,
                ReleaseYear = filmModel.ReleaseYear,
                Duration = filmModel.Duration,
                Platform = filmModel.Platform,
                CoverImageUrl = filmModel.CoverImageUrl,
                TrailerUrl = filmModel.TrailerUrl,
                TmdbId = filmModel.TmdbId, // Add TmdbId mapping
                Comments = filmModel.Comments.Select(c => c.ToCommentDto()).ToList()
            };
        }

        public static Films ToFilmFromCreateDto(this CreateFilmRequestDto filmDto)
        {
            return new Films
            {
                Name = filmDto.Name,
                IMDbRating = filmDto.IMDbRating,
                Description = filmDto.Description,
                Genre = filmDto.Genre,
                Director = filmDto.Director,
                LeadActors = filmDto.LeadActors,
                ReleaseYear = filmDto.ReleaseYear,
                Duration = filmDto.Duration,
                Platform = filmDto.Platform,
                CoverImageUrl = filmDto.CoverImageUrl, // Yeni özellik
                TrailerUrl= filmDto.TrailerUrl,
                TmdbId = filmDto.TmdbId
            };
        }
    }
}
