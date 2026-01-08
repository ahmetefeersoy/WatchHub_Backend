using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace api.Dtos.Comment
{
    public class CommentDto
    {
         public int Id { get; set; }

        public string Content { get; set; } = string.Empty;

        public int NumberOfLikes { get; set; }
        
        public bool ContainsSpoiler { get; set; } = false;

        public int StarRating { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.Now;
        public string CreatedBy { get; set; } = string.Empty;
        public int? FilmId { get; set; }

    }
}