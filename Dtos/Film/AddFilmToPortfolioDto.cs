using System.ComponentModel.DataAnnotations;

namespace api.Dtos.Film
{
    public class AddFilmToPortfolioDto
    {
        public int? TmdbId { get; set; }
        
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;
        
        [Range(0, 10)]
        public double IMDbRating { get; set; }
        
        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;
        
        [MaxLength(100)]
        public string Genre { get; set; } = string.Empty;
        
        [MaxLength(100)]
        public string Director { get; set; } = string.Empty;
        
        [MaxLength(200)]
        public string LeadActors { get; set; } = string.Empty;
        
        [Range(1900, 2100)]
        public int ReleaseYear { get; set; }
        
        [Range(1, 1000)]
        public int Duration { get; set; }
        
        [MaxLength(100)]
        public string Platform { get; set; } = string.Empty;
        
        [MaxLength(500)]
        public string? CoverImageUrl { get; set; }
        
        [MaxLength(500)]
        public string? TrailerUrl { get; set; }
    }
}
