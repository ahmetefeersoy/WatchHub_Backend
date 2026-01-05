using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Supabase;
using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;
using static Supabase.Gotrue.Constants;
using System;
using System.Threading.Tasks;

namespace api.Controllers
{
    [Route("api/auth")]
    [ApiController]
    [Authorize]
    public class SupabaseAuthController : ControllerBase
    {
        private readonly Supabase.Client _supabase;
        private readonly IConfiguration _configuration;

        public SupabaseAuthController(Supabase.Client supabase, IConfiguration configuration)
        {
            _supabase = supabase;
            _configuration = configuration;
        }

        /// <summary>
        /// Register a new user with email and password
        /// </summary>
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                var session = await _supabase.Auth.SignUp(request.Email, request.Password, new SignUpOptions
                {
                    Data = new Dictionary<string, object>
                    {
                        { "username", request.Username }
                    }
                });

                if (session?.User == null)
                {
                    return BadRequest(new { message = "Registration failed. Please check your email for verification." });
                }

                return Ok(new
                {
                    message = "Registration successful. Please check your email to verify your account.",
                    user = new
                    {
                        id = session.User.Id,
                        email = session.User.Email,
                        username = request.Username
                    },
                    session = new
                    {
                        accessToken = session.AccessToken,
                        refreshToken = session.RefreshToken,
                        expiresAt = session.ExpiresAt()
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Login with email and password
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var session = await _supabase.Auth.SignIn(request.Email, request.Password);

                if (session?.User == null)
                {
                    return Unauthorized(new { message = "Invalid email or password" });
                }

                return Ok(new
                {
                    user = new
                    {
                        id = session.User.Id,
                        email = session.User.Email,
                        username = session.User.UserMetadata?.ContainsKey("username") == true 
                            ? session.User.UserMetadata["username"] 
                            : session.User.Email
                    },
                    session = new
                    {
                        accessToken = session.AccessToken,
                        refreshToken = session.RefreshToken,
                        expiresAt = session.ExpiresAt()
                    }
                });
            }
            catch (Exception ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Login with Google OAuth (TEMPORARILY DISABLED)
        /// </summary>
        [HttpPost("login/google")]
        [AllowAnonymous]
        public async Task<IActionResult> LoginWithGoogle()
        {
            // Temporarily disabled - only email authentication is available
            return StatusCode(503, new { message = "Google OAuth is temporarily unavailable. Please use email authentication." });
        }

        /// <summary>
        /// Login with Apple OAuth (TEMPORARILY DISABLED)
        /// </summary>
        [HttpPost("login/apple")]
        [AllowAnonymous]
        public async Task<IActionResult> LoginWithApple()
        {
            // Temporarily disabled - only email authentication is available
            return StatusCode(503, new { message = "Apple Sign In is temporarily unavailable. Please use email authentication." });
        }

        /// <summary>
        /// OAuth callback handler
        /// </summary>
        [HttpGet("callback")]
        public async Task<IActionResult> OAuthCallback([FromQuery] string access_token, [FromQuery] string refresh_token)
        {
            try
            {
                if (string.IsNullOrEmpty(access_token))
                {
                    return BadRequest(new { message = "No access token provided" });
                }

                // Set the session
                await _supabase.Auth.SetSession(access_token, refresh_token);
                var user = _supabase.Auth.CurrentUser;

                if (user == null)
                {
                    return Unauthorized(new { message = "Invalid session" });
                }

                // Redirect to frontend with tokens
                var frontendUrl = _configuration["Frontend:Url"] ?? "exp://localhost:8081";
                return Redirect($"{frontendUrl}/auth/callback?access_token={access_token}&refresh_token={refresh_token}");
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Logout current user
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            try
            {
                await _supabase.Auth.SignOut();
                return Ok(new { message = "Logged out successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Refresh access token
        /// </summary>
        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                var session = await _supabase.Auth.RefreshSession();

                if (session == null)
                {
                    return Unauthorized(new { message = "Invalid refresh token" });
                }

                return Ok(new
                {
                    session = new
                    {
                        accessToken = session.AccessToken,
                        refreshToken = session.RefreshToken,
                        expiresAt = session.ExpiresAt()
                    }
                });
            }
            catch (Exception ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Get current user profile
        /// </summary>
        [HttpGet("profile")]
        [Authorize]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var user = _supabase.Auth.CurrentUser;

                if (user == null)
                {
                    return Unauthorized(new { message = "Not authenticated" });
                }

                return Ok(new
                {
                    id = user.Id,
                    email = user.Email,
                    username = user.UserMetadata?.ContainsKey("username") == true 
                        ? user.UserMetadata["username"] 
                        : user.Email,
                    createdAt = user.CreatedAt,
                    metadata = user.UserMetadata
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Update user profile
        /// </summary>
        [HttpPut("profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            try
            {
                var attributes = new UserAttributes
                {
                    Data = new Dictionary<string, object>()
                };

                if (!string.IsNullOrEmpty(request.Username))
                    attributes.Data["username"] = request.Username;
                
                if (!string.IsNullOrEmpty(request.FirstName))
                    attributes.Data["first_name"] = request.FirstName;
                
                if (!string.IsNullOrEmpty(request.LastName))
                    attributes.Data["last_name"] = request.LastName;
                
                if (!string.IsNullOrEmpty(request.Country))
                    attributes.Data["country"] = request.Country;

                var user = await _supabase.Auth.Update(attributes);

                if (user == null)
                {
                    return BadRequest(new { message = "Failed to update profile" });
                }

                return Ok(new
                {
                    id = user.Id,
                    email = user.Email,
                    metadata = user.UserMetadata
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Change password
        /// </summary>
        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                var attributes = new UserAttributes
                {
                    Password = request.NewPassword
                };

                var user = await _supabase.Auth.Update(attributes);

                if (user == null)
                {
                    return BadRequest(new { message = "Failed to change password" });
                }

                return Ok(new { message = "Password changed successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Reset password request
        /// </summary>
        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            try
            {
                var result = await _supabase.Auth.ResetPasswordForEmail(request.Email);
                return Ok(new { message = "Password reset email sent. Please check your inbox." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Delete user account
        /// </summary>
        [HttpDelete("account")]
        [Authorize]
        public async Task<IActionResult> DeleteAccount()
        {
            try
            {
                var user = _supabase.Auth.CurrentUser;
                if (user == null)
                {
                    return Unauthorized(new { message = "Not authenticated" });
                }

                // Note: Supabase doesn't have direct user deletion from client
                // This should be handled via Supabase Admin API or database trigger
                // For now, we'll sign out the user
                await _supabase.Auth.SignOut();

                return Ok(new { message = "Account deletion requested. Please contact support for confirmation." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }

    // DTOs
    public class RegisterRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class RefreshTokenRequest
    {
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class UpdateProfileRequest
    {
        public string? Username { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Country { get; set; }
    }

    public class ChangePasswordRequest
    {
        public string NewPassword { get; set; } = string.Empty;
    }

    public class ResetPasswordRequest
    {
        public string Email { get; set; } = string.Empty;
    }
}
