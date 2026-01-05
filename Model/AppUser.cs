using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace api.Model
{
    public class AppUser : IdentityUser
    {
        public List<Portfolio> Portfolios { get; set; } = new List<Portfolio>();
        public List<CommentLikePortfolio> CommentLikePortfolios { get; set; } = new List<CommentLikePortfolio>();

        public string FirstName { get; set; } = String.Empty;
        public string LastName { get; set; } = String.Empty;

        public string Country { get; set; } = String.Empty;
        public string ProfilImageUrl { get; set; } = String.Empty;
    }
}
