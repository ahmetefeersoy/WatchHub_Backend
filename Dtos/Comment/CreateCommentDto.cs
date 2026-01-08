using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace api.Dtos.Comment
{
    public class CreateCommentDto
    {

          [Required]
        //   [Range(1, 5, ErrorMessage = "Star rating must be between 1 and 5")]

          public int StarRating { get; set; }

          [Required]
          [MinLength(10,ErrorMessage = "Content must be 10 characters")]
          [MaxLength(250,ErrorMessage = "Content cannot be over 250 characters")]
          public string Content { get; set; } = string.Empty;
          
          public bool ContainsSpoiler { get; set; } = false;

    }
}