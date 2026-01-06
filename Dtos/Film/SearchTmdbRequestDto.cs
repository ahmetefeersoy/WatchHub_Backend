using System.ComponentModel.DataAnnotations;

namespace api.Dtos.Film
{
    public class SearchTmdbRequestDto
    {
        [Required]
        [MinLength(1, ErrorMessage = "Query must be at least 1 character")]
        public string Query { get; set; } = string.Empty;

        [Range(1, 1000)]
        public int Page { get; set; } = 1;

        [Range(1, 50)]
        public int Limit { get; set; } = 20;
    }
}
