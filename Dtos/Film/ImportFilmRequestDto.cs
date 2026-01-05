
namespace api.Dtos.Film
{
    public class ImportFilmRequestDto
    {
        public int Page { get; set; } = 1;
        public int Limit { get; set; } = 5;
        public int? GenreId { get; set; } = null; // TMDB genre ID for filtering
    }
}