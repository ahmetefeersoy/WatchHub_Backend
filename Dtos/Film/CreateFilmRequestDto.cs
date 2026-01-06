using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace api.Dtos.Film
{
    public class CreateFilmRequestDto
    {
        public int? TmdbId { get; set; }
        
        [Required]
        [MaxLength(50, ErrorMessage = "Name cannot be over 50 characters")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Range(0.1, 9.9)]
        public double IMDbRating { get; set; }

        [Required]
        [MaxLength(100, ErrorMessage = "Description cannot be over 100 characters")]
        public string Description { get; set; } = string.Empty;

        [Required]
        [MaxLength(30, ErrorMessage = "Genre cannot be over 30 characters")]
        public string Genre { get; set; } = string.Empty;

        [Required]
        [MaxLength(50, ErrorMessage = "Director cannot be over 50 characters")]
        public string Director { get; set; } = string.Empty;

        [Required]
        [MaxLength(60, ErrorMessage = "LeadActors cannot be over 60 characters")]
        public string LeadActors { get; set; } = string.Empty;

        [Required]
        [Range(1900, 2025)]
        public int ReleaseYear { get; set; }

        [Required]
        [Range(1, 500)]
        public int Duration { get; set; }

        [Required]
        [MaxLength(20, ErrorMessage = "Platform cannot be over 20 characters")]
        public string Platform { get; set; } = string.Empty;

        [MaxLength(500, ErrorMessage = "CoverImageUrl cannot be over 500 characters")]
        public string? CoverImageUrl { get; set; } // Yeni özellik

        [MaxLength(500, ErrorMessage = "TrailerUrl cannot be over 500 characters")]

        public string? TrailerUrl { get; set; } 

    }
}
