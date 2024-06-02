using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApi.DAL;
using WebApi.Model;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class GroupController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<GroupController> _logger;

        public GroupController(AppDbContext context, ILogger<GroupController> logger)
        {
            _context = context;
            _logger = logger;

        }

        // GET: api/Group
        [HttpGet]
        public async Task<ActionResult<IEnumerable<GroupModel>>> GetGroups()
        {
            try
            {
                return await _context.Groups.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"GroupController - Error occurred while retrieving groups: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }

        // GET: api/Group/1
        [HttpGet("{id}")]
        public async Task<ActionResult<GroupModel>> GetGroup(int id)
        {
            try
            {
                var group = await _context.Groups.FindAsync(id);

                if (group == null)
                {
                    _logger.LogWarning($"GroupController - Group with ID {id} not found.");
                    return NotFound();
                }

                _logger.LogInformation($"GroupController - Retrieved group with ID {id} successfully.");
                return group;
            }
            catch (Exception ex)
            {
                _logger.LogError($"GroupController - Error occurred while retrieving group with ID {id}: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }

        // POST: api/Group
        [HttpPost]
        public async Task<ActionResult<GroupModel>> PostGroup(GroupModel group)
        {
            try
            {
                var existingGroup = await _context.Groups.FirstOrDefaultAsync(g => g.GroupName == group.GroupName);
                if (existingGroup != null)
                {
                    return Ok("A group with the same name already exists.");
                }
                _context.Groups.Add(group);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"GroupController - Group with ID {group.GroupID} created successfully.");

                return Ok("Group created successfully!");

            }
            catch (Exception ex)
            {
                _logger.LogError($"GroupController - Error occurred while creating group: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }


        // PUT: api/Group/1
        [HttpPut("{id}")]
        public async Task<IActionResult> PutGroup(int id, GroupModel group)
        {
            if (id != group.GroupID)
            {
                _logger.LogWarning($"GroupController - Mismatch in IDs: Requested ID {id} does not match group ID {group.GroupID}.");
                return BadRequest();
            }

            _context.Entry(group).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation($"GroupController - Group with ID {id} updated successfully.");
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!GroupExists(id))
                {
                    _logger.LogWarning($"GroupController - Group with ID {id} not found.");
                    return NotFound();
                }
                else
                {
                    _logger.LogError($"GroupController - Error occurred while updating group with ID {id}: {ex.Message}");
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"GroupController - Error occurred while updating group with ID {id}: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }

            return NoContent();
        }


        // DELETE: api/Group/1
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGroup(int id)
        {
            try
            {
                var group = await _context.Groups.FindAsync(id);
                if (group == null)
                {
                    _logger.LogWarning($"GroupController - Group with ID {id} not found.");
                    return NotFound();
                }

                _context.Groups.Remove(group);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"GroupController - Group with ID {id} deleted successfully.");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError($"GroupController - Error occurred while deleting group with ID {id}: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }


        private bool GroupExists(int id)
        {
            var exists = _context.Groups.Any(e => e.GroupID == id);
            if (!exists)
            {
                _logger.LogWarning($"GroupController - Group with ID {id} does not exist.");
            }
            return exists;
        }

    }
}
