using System.Text.Json;
using System.Text.Json.Serialization;
using api.Model;

namespace api.Services
{
    public interface ITmdbService
    {
        Task<List<Films>> FetchPopularMoviesAsync(int page = 1, int limit = 5);
        Task<Films> FetchMovieDetailsAsync(int tmdbId);
    }

    public class TmdbService : ITmdbService
    {
        private readonly HttpClient _httpClient;
        private const string TMDB_API_KEY = "5e7d25b41269fe9475ddcdf31e6e7b74";
        private const string TMDB_BASE_URL = "https://api.themoviedb.org/3";
        private const string TMDB_IMAGE_BASE_URL = "https://image.tmdb.org/t/p";

        public TmdbService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<Films>> FetchPopularMoviesAsync(int page = 1, int limit = 5)
        {
            try
            {
                var url = $"{TMDB_BASE_URL}/movie/popular?api_key={TMDB_API_KEY}&page={page}&language=en-US";
                var response = await _httpClient.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"TMDB API error: {response.StatusCode}");
                }

                var content = await response.Content.ReadAsStringAsync();
                var tmdbResponse = JsonSerializer.Deserialize<TmdbMovieResponse>(content, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });

                var movies = new List<Films>();
                var results = tmdbResponse?.Results?.Take(limit) ?? new List<TmdbMovie>();

                foreach (var movie in results)
                {
                    var filmDetails = await FetchMovieDetailsAsync(movie.Id);
                    if (filmDetails != null)
                    {
                        movies.Add(filmDetails);
                    }
                }

                return movies;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching popular movies from TMDB: {ex.Message}", ex);
            }
        }

        public async Task<Films> FetchMovieDetailsAsync(int tmdbId)
        {
            try
            {
                var url = $"{TMDB_BASE_URL}/movie/{tmdbId}?api_key={TMDB_API_KEY}&language=en-US&append_to_response=videos,credits";
                var response = await _httpClient.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"TMDB API error: {response.StatusCode}");
                }

                var content = await response.Content.ReadAsStringAsync();
                var movieDetails = JsonSerializer.Deserialize<TmdbMovieDetails>(content, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });

                if (movieDetails == null)
                {
                    throw new Exception($"Failed to deserialize movie details for ID: {tmdbId}");
                }

                return MapTmdbMovieToFilm(movieDetails);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching movie details from TMDB: {ex.Message}", ex);
            }
        }

        private Films MapTmdbMovieToFilm(TmdbMovieDetails tmdbMovie)
        {
            var director = tmdbMovie.Credits?.Crew?.FirstOrDefault(c => c.Job == "Director")?.Name ?? "Unknown";
            var leadActors = string.Join(", ", tmdbMovie.Credits?.Cast?.Take(3).Select(a => a.Name) ?? new List<string>());
            var genres = string.Join(", ", tmdbMovie.Genres?.Select(g => g.Name) ?? new List<string>());
            var trailer = tmdbMovie.Videos?.Results?.FirstOrDefault(v => v.Type == "Trailer" && v.Site == "YouTube");

            int releaseYear = 0;
            if (!string.IsNullOrEmpty(tmdbMovie.ReleaseDate))
            {
                if (DateTime.TryParse(tmdbMovie.ReleaseDate, out DateTime parsedDate))
                {
                    releaseYear = parsedDate.Year;
                }
            }

            return new Films
            {
                TmdbId = tmdbMovie.Id,
                Name = tmdbMovie.Title ?? "Unknown",
                IMDbRating = tmdbMovie.VoteAverage,
                Description = tmdbMovie.Overview ?? "",
                Genre = !string.IsNullOrEmpty(genres) ? genres : "Unknown",
                Director = director,
                LeadActors = !string.IsNullOrEmpty(leadActors) ? leadActors : "Unknown",
                ReleaseYear = releaseYear,
                Duration = tmdbMovie.Runtime > 0 ? tmdbMovie.Runtime : 0,
                Platform = "TMDB",
                CoverImageUrl = !string.IsNullOrEmpty(tmdbMovie.PosterPath) 
                    ? $"{TMDB_IMAGE_BASE_URL}/w500{tmdbMovie.PosterPath}" 
                    : "",
                TrailerUrl = trailer != null 
                    ? $"https://www.youtube.com/watch?v={trailer.Key}" 
                    : null
            };
        }
    }

    // TMDB Response Models
    public class TmdbMovieResponse
    {
        [JsonPropertyName("page")]
        public int Page { get; set; }
        
        [JsonPropertyName("results")]
        public List<TmdbMovie> Results { get; set; } = new List<TmdbMovie>();
    }

    public class TmdbMovie
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;
        
        [JsonPropertyName("poster_path")]
        public string? PosterPath { get; set; }
    }

    public class TmdbMovieDetails
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;
        
        [JsonPropertyName("overview")]
        public string Overview { get; set; } = string.Empty;
        
        [JsonPropertyName("vote_average")]
        public double VoteAverage { get; set; }
        
        [JsonPropertyName("poster_path")]
        public string? PosterPath { get; set; }
        
        [JsonPropertyName("release_date")]
        public string? ReleaseDate { get; set; }
        
        [JsonPropertyName("runtime")]
        public int Runtime { get; set; }
        
        [JsonPropertyName("genres")]
        public List<TmdbGenre>? Genres { get; set; }
        
        [JsonPropertyName("videos")]
        public TmdbVideos? Videos { get; set; }
        
        [JsonPropertyName("credits")]
        public TmdbCredits? Credits { get; set; }
    }

    public class TmdbGenre
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    public class TmdbVideos
    {
        [JsonPropertyName("results")]
        public List<TmdbVideo> Results { get; set; } = new List<TmdbVideo>();
    }

    public class TmdbVideo
    {
        [JsonPropertyName("key")]
        public string Key { get; set; } = string.Empty;
        
        [JsonPropertyName("site")]
        public string Site { get; set; } = string.Empty;
        
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;
    }

    public class TmdbCredits
    {
        [JsonPropertyName("cast")]
        public List<TmdbCast> Cast { get; set; } = new List<TmdbCast>();
        
        [JsonPropertyName("crew")]
        public List<TmdbCrew> Crew { get; set; } = new List<TmdbCrew>();
    }

    public class TmdbCast
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    public class TmdbCrew
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonPropertyName("job")]
        public string Job { get; set; } = string.Empty;
    }
}