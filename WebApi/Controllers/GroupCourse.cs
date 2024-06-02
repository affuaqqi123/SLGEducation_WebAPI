using Microsoft.AspNetCore.Authorization;
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
    public class GroupCourseController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<GroupCourseController> _logger;

        public GroupCourseController(AppDbContext context, ILogger<GroupCourseController> logger)
        {
            _context = context;
            _logger = logger;

        }

        // GET: api/GroupCourse
        [HttpGet]
        public async Task<ActionResult<IEnumerable<GroupCourseModel>>> GetGroupCourses()
        {
            try
            {
                var groupCourses = await _context.GroupCourses.ToListAsync();
                _logger.LogInformation($"GroupCourseController - Retrieved {groupCourses.Count} group courses successfully.");
                return Ok(groupCourses);
            }
            catch (Exception ex)
            {
                _logger.LogError($"GroupCourseController - Error occurred while retrieving group courses: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }


        // GET: api/GroupCourse/1
        [HttpGet("{id}")]
        public async Task<ActionResult<GroupCourseModel>> GetGroupCourse(int id)
        {
            try
            {
                var groupCourse = await _context.GroupCourses.FindAsync(id);

                if (groupCourse == null)
                {
                    _logger.LogWarning($"GroupCourseController - Group course with ID {id} not found.");
                    return NotFound();
                }

                _logger.LogInformation($"GroupCourseController - Retrieved group course with ID {id} successfully.");
                return Ok(groupCourse);
            }
            catch (Exception ex)
            {
                _logger.LogError($"GroupCourseController - Error occurred while retrieving group course with ID {id}: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }


        // POST: api/GroupCourse
        [HttpPost]
        public async Task<ActionResult<GroupCourseModel>> PostGroupCourse(GroupCourseModel groupCourse)
        {
            try
            {
                _context.GroupCourses.Add(groupCourse);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"GroupCourseController - Group course with ID {groupCourse.GroupCourseID} created successfully.");

                return CreatedAtAction(nameof(GetGroupCourse), new { id = groupCourse.GroupCourseID }, groupCourse);
            }
            catch (Exception ex)
            {
                _logger.LogError($"GroupCourseController - Error occurred while creating group course: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }


        // PUT: api/GroupCourse/1
        [HttpPut("{id}")]
        public async Task<IActionResult> PutGroupCourse(int id, GroupCourseModel groupCourse)
        {
            if (id != groupCourse.GroupCourseID)
            {
                _logger.LogWarning($"GroupCourseController - Mismatch in IDs: Requested ID {id} does not match group course ID {groupCourse.GroupCourseID}.");
                return BadRequest();
            }

            _context.Entry(groupCourse).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation($"GroupCourseController - Group course with ID {id} updated successfully.");
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!GroupCourseExists(id))
                {
                    _logger.LogWarning($"GroupCourseController - Group course with ID {id} not found.");
                    return NotFound();
                }
                else
                {
                    _logger.LogError($"GroupCourseController - Error occurred while updating group course with ID {id}: {ex.Message}");
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"GroupCourseController - Error occurred while updating group course with ID {id}: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }

            return NoContent();
        }


        // DELETE: api/GroupCourse/1
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGroupCourse(int id)
        {
            try
            {
                var groupCourse = await _context.GroupCourses.FindAsync(id);
                if (groupCourse == null)
                {
                    _logger.LogWarning($"GroupCourseController - Group course with ID {id} not found.");
                    return NotFound();
                }

                _context.GroupCourses.Remove(groupCourse);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"GroupCourseController - Group course with ID {id} deleted successfully.");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError($"GroupCourseController - Error occurred while deleting group course with ID {id}: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }


        private bool GroupCourseExists(int id)
        {
            var exists = _context.GroupCourses.Any(e => e.GroupCourseID == id);
            if (!exists)
            {
                _logger.LogWarning($"GroupCourseController - Group course with ID {id} does not exist.");
            }
            return exists;
        }

    }
}
