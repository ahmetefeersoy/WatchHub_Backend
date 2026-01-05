using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace api.Model
{
    [Table("Films")]
    public class Films
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

        public string CoverImageUrl { get; set; } = string.Empty; 

        public string? TrailerUrl { get; set; } 

        public int? TmdbId { get; set; }

        public List<Comment> Comments { get; set; } = new List<Comment>();
        public List<Portfolio> Portfolios { get; set; } = new List<Portfolio>();
    }
}
