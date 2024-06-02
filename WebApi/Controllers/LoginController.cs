using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using WebApi.Model;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebApi.DAL;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using System;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using NuGet.Common;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Options;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using Microsoft.Extensions.Logging;
using Serilog;
using Microsoft.AspNetCore.SignalR;



namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
   
    public class LoginController : ControllerBase
    {

        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<LoginController> _logger;
        public LoginController(AppDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration config,
            ILogger<LoginController> logger)
        {
            _context = context;
            _config = config;
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;

        }

        [AllowAnonymous]
        // POST: api/Login  
        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            //_logger.LogInformation("{UserId}", model.Username);
            string uname = model.Username;

            try
            {
                
                ApplicationUser au = new ApplicationUser();

                var user = await _context.Users
                    .Where(u => u.Username == model.Username && u.Password == model.Password)
                    .FirstOrDefaultAsync();

                if (user == null)
                {

                    //_logger.LogError("LoginController - Invalid Username or password");                    
                    //_logger.LogError("LoginController - Invalid {UserName} or password", uname);

                    Log.ForContext("UserName", uname).Error("LoginController - Invalid Username or password");

                    return NotFound("Invalid username or password");
                }


                var claims = new List<Claim>
            { 
            //Subject of the JWT
            //new Claim(JwtRegisteredClaimNames.Sub, _config["Jwt:Subject"]),
            new Claim(JwtRegisteredClaimNames.Name, model.Username),            
            // Unique Id for all Jwt tokes
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                        // Issued at
            new Claim(JwtRegisteredClaimNames.Iat, DateTime.UtcNow.ToString()),
            new Claim("UserName", "Scandanavian"),
             new Claim("DisplayName", "Skeidar Living Group")

            };
                var token = BuildToken(claims);
                var refreshToken = GenerateRefreshToken();

                _ = int.TryParse(_config["JwtToken:RefreshTokenValidityInDays"], out int refreshTokenValidityInDays);

                au.RefreshToken = refreshToken;
                au.RefreshTokenExpiryTime = DateTime.Now.AddDays(refreshTokenValidityInDays);

                await _userManager.UpdateAsync(au);

                // Return the user
                return Ok(new
                {
                    token,
                    RefreshToken = refreshToken,
                    role = user.Role,
                    userName = user.Username,
                    userID = user.UserID
                    //Expiration = token.ValidTo
                    
            }
                );
            }
            catch (Exception ex)
            {
                //_logger.LogError($"LoginController - Error occurred during login for username '{model.Username}': {ex.Message}");

                Log.ForContext("UserName", uname).Error($"LoginController - Error occurred during login for username '{model.Username}': {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }

        private string BuildToken(List<Claim> claims)
        {
            try
            {
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JwtToken:SecretKey"]));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                _ = int.TryParse(_config["JwtToken:TokenValidityInMinutes"], out int tokenValidityInMinutes);
                var token = new JwtSecurityToken(_config["JwtToken:Issuer"],
                                                 _config["JwtToken:Audience"],
                                                 claims,
                                                 expires: DateTime.Now.AddMinutes(tokenValidityInMinutes),
                                                 signingCredentials: creds);
                
                _logger.LogInformation("LoginController - JWT token generated successfully.");
                return new JwtSecurityTokenHandler().WriteToken(token);
            }
            catch(Exception ex) 
            {
                _logger.LogError($"LoginController - Error occurred while generating JWT token: {ex.Message}");
                throw; // Propagate the exception
            }
        }


        
        [HttpPost]
        [Route("refresh-token")]
        public async Task<IActionResult> RefreshToken(TokenModel tokenModel)
        {
            try
            {
                ApplicationUser au = new ApplicationUser();
                if (tokenModel is null)
                {
                    _logger.LogWarning("LoginController - Invalid client request: Token model is null.");
                    return BadRequest("Invalid client request");
                }

                string? accessToken = tokenModel.AccessToken;
                string? refreshToken = tokenModel.RefreshToken;

                var principal = GetPrincipalFromExpiredToken(accessToken);
                if (principal == null)
                {
                    _logger.LogWarning("LoginController - Invalid access token or refresh token.");
                    return BadRequest("Invalid access token or refresh token");
                }
                //string username = principal.Claims[0];


                var username = principal.Claims.ElementAt(0).Value;

                //var user = await _userManager.FindByNameAsync(username);
                //if (username == null || au.RefreshToken != refreshToken || au.RefreshTokenExpiryTime <= DateTime.Now)


                if (username == null || au.RefreshToken == refreshToken || au.RefreshTokenExpiryTime >= DateTime.Now)
                {
                    _logger.LogWarning("LoginController - Invalid access token or refresh token.");
                    return BadRequest("Invalid access token or refresh token");
                }

                var newAccessToken = BuildToken(principal.Claims.ToList());
                var newRefreshToken = GenerateRefreshToken();

                au.RefreshToken = newRefreshToken;
                await _userManager.UpdateAsync(au);

                _logger.LogInformation($"LoginController - Access token refreshed for user '{username}'.");

                return new ObjectResult(new
                {
                    newAccessToken,
                    refreshToken = newRefreshToken
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"LoginController - Error occurred while refreshing token: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");

            }
        }

        

        [Authorize]
        [HttpPost]
        [Route("revoke/{username}")]
        public async Task<IActionResult> Revoke(string username)
        {
            try
            {
                var user = await _userManager.FindByNameAsync(username);
                if (user == null)
                {
                    _logger.LogWarning($"LoginController - Invalid user name '{username}' for token revocation.");
                    return BadRequest("Invalid user name");
                }

                user.RefreshToken = null;
                await _userManager.UpdateAsync(user);
                _logger.LogInformation($"LoginController - Refresh token revoked for user '{username}'.");


                return NoContent();
            }
            catch(Exception ex)
            {
                _logger.LogError($"LoginController - Error occurred while revoking refresh token for user '{username}': {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");

            }
        }

        [Authorize]
        [HttpPost]
        [Route("revoke-all")]
        public async Task<IActionResult> RevokeAll()
        {
            try
            {
                var users = _userManager.Users.ToList();
                foreach (var user in users)
                {
                    user.RefreshToken = null;
                    await _userManager.UpdateAsync(user);
                }
                _logger.LogInformation("LoginController - All refresh tokens revoked successfully.");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError($"LoginController - Error occurred while revoking all refresh tokens: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }


        private static string GenerateRefreshToken()
        {

            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        private ClaimsPrincipal? GetPrincipalFromExpiredToken(string? token)
        {
            try
            {
                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = false,
                    ValidateIssuer = false,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JwtToken:SecretKey"])),
                    ValidateLifetime = false
                };

                var tokenHandler = new JwtSecurityTokenHandler();
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);

                var name = securityToken.Id.ToString();
                if (securityToken is not JwtSecurityToken jwtSecurityToken || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    _logger.LogError("LoginController - Invalid token.");
                    throw new SecurityTokenException("Invalid token");
                }

                _logger.LogInformation("LoginController - Token validated successfully.");
                return principal;
            }
            catch (Exception ex)
            {
                _logger.LogError($"LoginController - Error occurred while validating token: {ex.Message}");
                return null;

            }

        }

        [HttpGet]

        public async Task<IEnumerable<string>> Get()
        {
            try
            {
                var accessToken = await HttpContext.GetTokenAsync("access_token");


                return new string[] { accessToken };
            }
            catch (Exception ex)
            {
                _logger.LogError($"LoginController - Error occurred while retrieving access token: {ex.Message}");
                throw; // Propagate the exception
            }
        }

        // Method to hash the password using SHA-256 and a salt
        private string HashPassword(string password, string salt)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] saltBytes = Convert.FromBase64String(salt);
                byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
                byte[] combinedBytes = new byte[saltBytes.Length + passwordBytes.Length];

                Array.Copy(saltBytes, combinedBytes, saltBytes.Length);
                Array.Copy(passwordBytes, 0, combinedBytes, saltBytes.Length, passwordBytes.Length);

                byte[] hashedBytes = sha256.ComputeHash(combinedBytes);
                return Convert.ToBase64String(hashedBytes);
            }
        }

        private string GenerateSalt()
        {
            byte[] salt = new byte[16]; // You can adjust the length of the salt as needed
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(salt);
            }
            return Convert.ToBase64String(salt);
        }



    }
}


