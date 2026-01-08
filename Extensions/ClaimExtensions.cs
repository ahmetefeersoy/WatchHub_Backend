using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace api.Extensions
{
    public static class ClaimExtensions
    {
        public static string GetUsername(this ClaimsPrincipal user){
            // Önce email'i bul
            var emailClaim = user.Claims.SingleOrDefault(x => x.Type == ClaimTypes.Email || x.Type == "email");
            if (emailClaim != null)
                return emailClaim.Value;
            
            // Eski sistem için givenname
            var claim = user.Claims.SingleOrDefault(x => x.Type.Equals("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname"));
            if (claim != null)
                return claim.Value;
                
            return null;
        }
    }
}