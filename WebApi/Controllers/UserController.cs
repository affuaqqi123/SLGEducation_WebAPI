using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using System.Net;
using WebApi.DAL;
using WebApi.Model;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;


namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly EmailModel _configuration;
        private readonly IConfiguration _config;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<UserController> _logger; 

        public UserController(AppDbContext context,
            IOptions<EmailModel> configuration,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration config,
            ILogger<UserController> logger)
        {
            _context = context;
            _configuration = configuration.Value;
            _config = config;
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        public class EmailInputModel
        {
            public string RecipientEmail { get; set; }
            public string Username { get; set; }
            public string Password { get; set; }
        }
        //[HttpPost("slgemail")]
        //public IActionResult SendEmail(string recipientEmail, string username, string password)
        //{
        //    string bodyTemplate = _configuration.EmailBody;
        //    bodyTemplate = bodyTemplate.Replace("{RecipientEmail}", recipientEmail)
        //                              .Replace("{UserName}", username)
        //                              .Replace("{Password}", password);

           // sending as an object

           [HttpPost("slgemail")]
            public IActionResult SendEmail([FromBody] EmailInputModel emailinputs)
            {

                string recipientEmail = emailinputs.RecipientEmail;
                string username = emailinputs.Username;
                string password = emailinputs.Password;

                string bodyTemplate = _configuration.EmailBody;
                bodyTemplate = bodyTemplate.Replace("{RecipientEmail}", recipientEmail)
                                          .Replace("{UserName}", username)
                                          .Replace("{Password}", password);

                try
            {
                //string recipientEmail = request.Email;
                SmtpClient client = new SmtpClient(_configuration.SmtpClient)
                {
                    Port = _configuration.SmtpPort,
                    Credentials = new NetworkCredential(_configuration.SenderEmail, _configuration.SenderPassword),
                    EnableSsl = true,
                    //Timeout = 100000,
            };

                MailMessage mailMessage = new MailMessage
                {
                    From = new MailAddress(_configuration.SenderEmail),
                    Subject = _configuration.EmailSubject,
                    //Body = $"Hello {username},</p>" +
                    //        "<br><br>" +
                    //        "<p> Click the following link to access training course: </p>" +
                    //        "<p> https://www.skeidar.no/ </p>" +
                    //        "<p> Please use the below Credentials to login into the SLGEducation Application</p>" +
                    //        $"<p>Your Username is <b>{recipientEmail}</b></p>" +
                    //        $"<p>Your Password is <b>{password}</b></p>" +
                    //        "<br><br><br><br><br><br>" +
                    //        "Thanks and Regards," +
                    //        "<br>Skeidar Living Group",
                    Body = bodyTemplate,
                    IsBodyHtml = true,
                };

                mailMessage.To.Add(recipientEmail);

                client.Send(mailMessage);

                _logger.LogInformation("UserController - Email sent successfully");
                return Ok(new { Message = "Email sent successfully" });
            }
            catch (Exception ex)
            {
                // Log the exception
                _logger.LogError(ex, "UserController - An error occurred while sending email");
               // return StatusCode(500, "An error occurred while sending email");
               return BadRequest(ex.Message);

            }
        }
        public class EmailRequest
        {
            public string Email { get; set; }
        }
        // GET: api/User
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserModel>>> GetUsers()
        {
            try
            {
                var users = await _context.Users.ToListAsync();
                if (users.Count == 0)
                {
                    return Ok("No users found.");
                }
                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserController - An error occurred while fetching users");
                return StatusCode(500, "An error occurred while fetching users");
            }
        }

        // GET: api/User/1
        [HttpGet("{id}")]
        public async Task<ActionResult<UserModel>> GetUser(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);

                if (user == null)
                {
                    return NotFound();
                }

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserController - An error occurred while fetching user with ID {UserId}", id);
                return StatusCode(500, "An error occurred while fetching user");
            }
        }

        // POST: api/User
        [HttpPost]
        public async Task<IActionResult> PostUser([FromBody] UserModel model)
        {
            try
            {
                var existingUsername = await _context.Users.FirstOrDefaultAsync(u => u.Username == model.Username);
                if (existingUsername != null)
                {
                    return Ok("This Username is already taken.");
                }

                var existingEmail = await _context.Users.FirstOrDefaultAsync(u => u.UserEmail == model.UserEmail);
                if (existingEmail != null)
                {
                    return Ok("This EmailID is already registered.");
                }

                _context.Users.Add(model);
                await _context.SaveChangesAsync();

                CreatedAtAction(nameof(GetUser), new { id = model.UserID }, model);
                ApplicationUser au = new();

                au.Email = model.UserEmail;
                au.SecurityStamp = Guid.NewGuid().ToString();
                au.UserName = model.Username;

                return Ok("User created successfully!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserController - An error occurred while processing user answer.");
                return StatusCode(500, "Internal server error");
            }
        }

        // PUT: api/User/1
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> PutUser(int id, UserModel user)
        {
            if (id != user.UserID)
            {
                return BadRequest();
            }

            _context.Entry(user).State = EntityState.Modified;

            ApplicationUser au = new();

            au.UserName = user.Username;

            try
            {
                await _context.SaveChangesAsync();
                await _userManager.UpdateAsync(au);
                return NoContent();

            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserController - An error occurred while updating user with ID {UserId}", id);
                return StatusCode(500, "An error occurred while updating user");
            }


        }


        // DELETE: api/User/1
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    _logger.LogWarning("User with ID {UserId} not found.", id);
                    return NotFound();
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                var identityUser = await _userManager.FindByIdAsync(id.ToString());
                if (identityUser == null)
                {
                    _logger.LogWarning("User with ID {UserId} not found in ASP.NET Identity tables.", id);
                    return NoContent();
                }

                var result = await _userManager.DeleteAsync(identityUser);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogError($"Failed to delete user with ID {id} from ASP.NET Identity tables: {errors}");
                    return StatusCode(500, $"Failed to delete user from ASP.NET Identity tables: {errors}");
                }
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserController - An error occurred while deleting user with ID {UserId}", id);
                return StatusCode(500, "An error occurred while deleting user");
            }
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.UserID == id);
        }
    }
}


