using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace api.Middleware
{
    public class SupabaseJwtValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;

        public SupabaseJwtValidationMiddleware(RequestDelegate next, IConfiguration configuration)
        {
            _next = next;
            _configuration = configuration;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

            if (token != null)
            {
                try
                {
                    // Validate Supabase JWT token
                    var supabaseUrl = _configuration["Supabase:Url"];
                    var supabaseKey = _configuration["Supabase:Key"];

                    if (!string.IsNullOrEmpty(supabaseUrl) && !string.IsNullOrEmpty(supabaseKey))
                    {
                        // Decode JWT to check if it's a Supabase token
                        var handler = new JwtSecurityTokenHandler();
                        var jwtToken = handler.ReadJwtToken(token);

                        // Check if token is from Supabase (has iss claim with supabase.co)
                        var issuer = jwtToken.Claims.FirstOrDefault(c => c.Type == "iss")?.Value;
                        
                        if (issuer != null && issuer.Contains("supabase"))
                        {
                            // This is a Supabase token, validate it
                            var supabaseJwtSecret = GetSupabaseJwtSecret(supabaseUrl);
                            
                            var validationParameters = new TokenValidationParameters
                            {
                                ValidateIssuerSigningKey = true,
                                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(supabaseJwtSecret)),
                                ValidateIssuer = true,
                                ValidIssuer = issuer,
                                ValidateAudience = false, // Supabase doesn't use aud claim consistently
                                ValidateLifetime = true,
                                ClockSkew = TimeSpan.Zero
                            };

                            try
                            {
                                var principal = handler.ValidateToken(token, validationParameters, out var validatedToken);
                                
                                // Log all claims for debugging
                                Console.WriteLine("🔐 JWT Claims:");
                                foreach (var claim in principal.Claims)
                                {
                                    Console.WriteLine($"  - {claim.Type}: {claim.Value}");
                                }
                                
                                // user_metadata'dan username'i çıkar
                                var userMetadataClaim = principal.Claims.FirstOrDefault(c => c.Type == "user_metadata");
                                if (userMetadataClaim != null)
                                {
                                    try
                                    {
                                        var metadata = System.Text.Json.JsonDocument.Parse(userMetadataClaim.Value);
                                        if (metadata.RootElement.TryGetProperty("username", out var usernameProp))
                                        {
                                            var username = usernameProp.GetString();
                                            Console.WriteLine($"✅ Username from metadata: {username}");
                                            
                                            // Username'i claim olarak ekle
                                            var claims = principal.Claims.ToList();
                                            claims.Add(new Claim("username", username));
                                            var identity = new ClaimsIdentity(claims, "Supabase");
                                            principal = new ClaimsPrincipal(identity);
                                        }
                                    }
                                    catch (Exception metaEx)
                                    {
                                        Console.WriteLine($"⚠️ Failed to parse user_metadata: {metaEx.Message}");
                                    }
                                }
                                
                                context.User = principal;
                            }
                            catch (SecurityTokenExpiredException)
                            {
                                context.Response.StatusCode = 401;
                                await context.Response.WriteAsJsonAsync(new { message = "Token has expired" });
                                return;
                            }
                            catch (Exception ex)
                            {
                                // Token validation failed
                                Console.WriteLine($"Supabase JWT validation failed: {ex.Message}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"JWT processing error: {ex.Message}");
                }
            }

            await _next(context);
        }

        private string GetSupabaseJwtSecret(string supabaseUrl)
        {
            // The JWT secret for Supabase is typically the same as the service role key
            // For development, you can use the anon key, but for production you should use the JWT secret
            // You need to get this from your Supabase project settings under API > JWT Settings
            
            var jwtSecret = _configuration["Supabase:JwtSecret"];
            
            if (!string.IsNullOrEmpty(jwtSecret))
            {
                return jwtSecret;
            }

            // Fallback to using the anon key (not recommended for production)
            return _configuration["Supabase:Key"] ?? string.Empty;
        }
    }

    public static class SupabaseJwtValidationMiddlewareExtensions
    {
        public static IApplicationBuilder UseSupabaseJwtValidation(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SupabaseJwtValidationMiddleware>();
        }
    }
}
