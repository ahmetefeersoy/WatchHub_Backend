using System;
using System.Collections.Generic;
using api.Dtos.Comment;

namespace api.Dtos.Film
{
    public class FilmDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public double IMDbRating { get; set; }

        public string Description { get; set; } = string.Empty;

        public string Genre { get; set; } = string.Empty;

        public string Director { get; set; } = string.Empty;

        public string LeadActors { get; set; } = string.Empty;

        public int ReleaseYear { get; set; }

        public int Duration { get; set; }

        public string Platform { get; set; } = string.Empty;

        public string? CoverImageUrl { get; set; } 

        public string? TrailerUrl { get; set; } 

        public int? TmdbId { get; set; }

        public List<CommentDto> Comments { get; set; } = new List<CommentDto>();
    }
}
