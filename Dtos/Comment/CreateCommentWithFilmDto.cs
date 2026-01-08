using System.ComponentModel.DataAnnotations;

namespace api.Dtos.Comment
{
    public class CreateCommentWithFilmDto
    {
        // Film bilgileri (TmdbId varsa film otomatik oluşturulacak)
        public int? TmdbId { get; set; }
        
        public int? FilmId { get; set; }  // Mevcut film için
        
        [MaxLength(200)]
        public string? FilmName { get; set; }
        
        [Range(0, 10)]
        public double? IMDbRating { get; set; }
        
        [MaxLength(1000)]
        public string? Description { get; set; }
        
        [MaxLength(100)]
        public string? Genre { get; set; }
        
        [MaxLength(100)]
        public string? Director { get; set; }
        
        [MaxLength(200)]
        public string? LeadActors { get; set; }
        
        [Range(1900, 2100)]
        public int? ReleaseYear { get; set; }
        
        [Range(1, 1000)]
        public int? Duration { get; set; }
        
        [MaxLength(100)]
        public string? Platform { get; set; }
        
        [MaxLength(500)]
        public string? CoverImageUrl { get; set; }
        
        [MaxLength(500)]
        public string? TrailerUrl { get; set; }
        
        // Yorum bilgileri
        [Required]
        public int StarRating { get; set; }

        [Required]
        [MinLength(10, ErrorMessage = "Content must be at least 10 characters")]
        [MaxLength(250, ErrorMessage = "Content cannot exceed 250 characters")]
        public string Content { get; set; } = string.Empty;
        
        public bool ContainsSpoiler { get; set; } = false;
    }
}
