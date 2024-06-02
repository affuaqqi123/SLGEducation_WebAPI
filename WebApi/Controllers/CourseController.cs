using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
    public class CourseController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CourseController> _logger;

        public CourseController(AppDbContext context, ILogger<CourseController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/Course
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CourseModel>>> GetCourses()
        {
            try
            {
                var courses = await _context.Courses.ToListAsync();
                return Ok(courses);
            }
            catch (Exception ex)
            {
                _logger.LogError("CourseController - Error occurred while getting courses: " + ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }

        }

        // GET: api/Course/1
        [HttpGet("{id}")]
        public async Task<ActionResult<CourseModel>> GetCourse(int id)
        {
            try
            {
                var course = await _context.Courses.FindAsync(id);

                if (course == null)
                {
                    _logger.LogError($"CourseController - Course with ID {id} not found.");
                    //_logger.LogWarning("Course with the given ID  not found.");
                    return NotFound();
                }

                return course;
            }
            catch (Exception ex)
            {
                _logger.LogError($"CourseController - Error occurred while getting course with ID {id}: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }

        // GET: api/Course/GetCoursesForUser/{userId}
        [HttpGet("GetCoursesForUser/{userId}")]
        public async Task<ActionResult<IEnumerable<CourseModel>>> GetCoursesForUser(int userId)
        {
            try
            {
                // Get the group ID for the user from the UserGroupModel
                var userGroup = await _context.UserGroup.FirstOrDefaultAsync(ug => ug.UserID == userId);
                if (userGroup == null)
                {
                    // If userGroup is null, user doesn't belong to any group
                    return Ok("User has not been assigned any Courses."); // Return message
                }

                // Get the group name using the group ID
                var group = await _context.Groups.FindAsync(userGroup.GroupID);
                if (group == null)
                {
                    return NotFound("Group not found.");
                }

                // Get the courses where GroupName matches
                var courses = await _context.Courses
                    .Where(c => c.GroupName == group.GroupName)
                    .ToListAsync();

                return Ok(courses);
            }
            catch (Exception ex)
            {
                // Log and return error message
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }


        // POST: api/Course
        [HttpPost]
        public async Task<ActionResult<CourseModel>> PostCourse(CourseModel course)
        {
            try
            {
                var existingCourse = await _context.Courses.FirstOrDefaultAsync(c => c.CourseName == course.CourseName);
                if (existingCourse != null)
                {
                    return Ok("A course with the same name already exists.");
                }

                _context.Courses.Add(course);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"CourseController - Course added successfully with ID {course.CourseID}");

                return Ok("Course Created Successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError($"CourseController - Error occurred while adding course: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }

        // PUT: api/Course/1
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCourse(int id, CourseModel course)
        {
            if (id != course.CourseID)
            {
                _logger.LogError($"CourseController - Course ID mismatch: {id} provided in URL does not match the CourseID {course.CourseID} provided in the request body.");
                return BadRequest();
            }

            _context.Entry(course).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation($"CourseController - Course with ID {id} updated successfully.");
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!CourseExists(id))
                {
                    _logger.LogWarning($"CourseController - Course with ID {id} not found while trying to update.");
                    return NotFound();
                }
                else
                {
                    _logger.LogError($"CourseController - Concurrency exception occurred while updating course with ID {id}: {ex.Message}");
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/Course/1
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            try
            {
                var course = await _context.Courses.FindAsync(id);
                if (course == null)
                {
                    _logger.LogWarning($"CourseController - Course with ID {id} not found while attempting to delete.");
                    return NotFound();
                }

                _context.Courses.Remove(course);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"CourseController - Course with ID {id} deleted successfully.");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError($"CourseController - Error occurred while deleting course with ID {id}: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }

        private bool CourseExists(int id)
        {
            return _context.Courses.Any(e => e.CourseID == id);
        }
    }
}
