using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApi.DAL;
using WebApi.Model;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserCourseController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<UserCourseController> _logger;

        public UserCourseController(AppDbContext context, ILogger<UserCourseController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/UserCourse
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserCourseModel>>> GetUserCourses()
        {
            return await _context.UserCourse.ToListAsync();
        }

        // GET: api/UserCourse/5
        [HttpGet("{id}")]
        public async Task<ActionResult<UserCourseModel>> GetUserCourse(int id)
        {
            var userCourse = await _context.UserCourse.FindAsync(id);

            if (userCourse == null)
            {
                return NotFound();
            }

            return userCourse;
        }

        // POST: api/UserCourse
        [HttpPost]
        public async Task<ActionResult<UserCourseModel>> PostUserCourse(UserCourseModel userCourse)
        {
            // Check if there is an existing record for the given userID and courseID
            var existingUserCourse = await _context.UserCourse
                .FirstOrDefaultAsync(uc => uc.UserID == userCourse.UserID && uc.CourseID == userCourse.CourseID);

            if (existingUserCourse != null)
            {
                // If a record already exists, return a conflict response indicating the duplication
                return Conflict("A previous record already exists for this user and course.");
            }

            // If no previous record exists, add the new userCourse and save changes
            _context.UserCourse.Add(userCourse);
            await _context.SaveChangesAsync();

            // Return a created response with the newly created userCourse
            return CreatedAtAction(nameof(GetUserCourse), new { id = userCourse.UserID }, userCourse);
        }


        // PUT: api/UserCourse/{userId}/{courseId}
        [HttpPut("UpdateUserCourse")]
        public async Task<IActionResult> PutUserCourse(int userId, int courseId, bool isCourseCompleted, DateTime endTime)
        {
            var userCourse = await _context.UserCourse.FirstOrDefaultAsync(uc => uc.UserID == userId && uc.CourseID == courseId);

            if (userCourse == null)
            {
                return NotFound();
            }

            // Update the fields
            userCourse.IsCourseCompleted = isCourseCompleted;
            userCourse.EndTime = endTime;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserCourseExists(userCourse.UserCourseID))
                {
                    return NotFound();
                }
                else
                {
                    _logger.LogError($"UserCourseController - Error occurred while updating UserCourse");
                    throw;
                }
            }

            return NoContent();
        }


        // DELETE: api/UserCourse/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUserCourse(int id)
        {
            var userCourse = await _context.UserCourse.FindAsync(id);
            if (userCourse == null)
            {
                return NotFound();
            }

            _context.UserCourse.Remove(userCourse);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool UserCourseExists(int id)
        {
            return _context.UserCourse.Any(e => e.UserID == id);
        }
    }
}
