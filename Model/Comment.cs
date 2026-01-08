using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace api.Model
{
    [Table("Comments")]

    public class Comment
    {
        public int Id { get; set; }

        public int StarRating { get; set; }
        
        public int NumberOfLikes { get; set; }
        public string Content { get; set; } = string.Empty;
        public bool ContainsSpoiler { get; set; } = false;

        public DateTime CreatedOn { get; set; } = DateTime.Now;
        public int? FilmId { get; set; }

        public Films? Film { get; set; } 
        public string AppUserId { get; set; }   
        public AppUser AppUser { get; set; }
        public List<CommentLikePortfolio> CommentLikePortfolios { get; set; } = new List<CommentLikePortfolio>();


        
        }
}