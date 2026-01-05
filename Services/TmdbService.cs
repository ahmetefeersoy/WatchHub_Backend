using System.Text.Json;
using System.Text.Json.Serialization;
using api.Model;

namespace api.Services
{
    public interface ITmdbService
    {
        Task<List<Films>> FetchPopularMoviesAsync(int page = 1, int limit = 5, int? genreId = null);
        Task<Films> FetchMovieDetailsAsync(int tmdbId);
    }

    public class TmdbService : ITmdbService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _accessToken;
        private readonly string _baseUrl;
        private readonly string _imageBaseUrl;

        public TmdbService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["TMDB:ApiKey"] ?? throw new InvalidOperationException("TMDB API Key not configured");
            _accessToken = configuration["TMDB:AccessToken"] ?? "";
            _baseUrl = configuration["TMDB:BaseUrl"] ?? "https://api.themoviedb.org/3";
            _imageBaseUrl = configuration["TMDB:ImageBaseUrl"] ?? "https://image.tmdb.org/t/p";
            
            // Set headers for TMDB API v4 (Bearer token preferred)
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "WatchHub/1.0");
            
            // Use Bearer token if available, otherwise fall back to API key
            if (!string.IsNullOrEmpty(_accessToken))
            {
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessToken}");
            }
        }

        public async Task<List<Films>> FetchPopularMoviesAsync(int page = 1, int limit = 5, int? genreId = null)
        {
            try
            {
                // Use discover endpoint if genre is specified, otherwise use popular
                string endpoint;
                if (genreId.HasValue)
                {
                    // Use Bearer token if available, otherwise use api_key parameter
                    endpoint = !string.IsNullOrEmpty(_accessToken)
                        ? $"/discover/movie?with_genres={genreId.Value}&page={page}&language=en-US&sort_by=popularity.desc"
                        : $"/discover/movie?api_key={_apiKey}&with_genres={genreId.Value}&page={page}&language=en-US&sort_by=popularity.desc";
                }
                else
                {
                    endpoint = !string.IsNullOrEmpty(_accessToken)
                        ? $"/movie/popular?page={page}&language=en-US"
                        : $"/movie/popular?api_key={_apiKey}&page={page}&language=en-US";
                }

                var url = $"{_baseUrl}{endpoint}";
                    
                var response = await _httpClient.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"TMDB API error: {response.StatusCode}, Details: {errorContent}");
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
                // Use Bearer token if available, otherwise use api_key parameter
                var url = !string.IsNullOrEmpty(_accessToken)
                    ? $"{_baseUrl}/movie/{tmdbId}?language=en-US&append_to_response=videos,credits"
                    : $"{_baseUrl}/movie/{tmdbId}?api_key={_apiKey}&language=en-US&append_to_response=videos,credits";
                    
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
                    ? $"{_imageBaseUrl}/w500{tmdbMovie.PosterPath}" 
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