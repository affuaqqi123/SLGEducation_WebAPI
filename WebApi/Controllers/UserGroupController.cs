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
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserGroupController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<UserGroupController> _logger;

        public UserGroupController(AppDbContext context, ILogger<UserGroupController> logger)
        {
            _context = context;
            _logger = logger;

        }

        // GET: api/UserGroup
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserGroupModel>>> GetUserGroups()
        {
            try
            {
                var userGroups = await _context.UserGroup.ToListAsync();
                if (userGroups.Count == 0)
                {
                    return Ok("No user groups found.");
                }
                return Ok(userGroups);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserGroupController - An error occurred while retrieving user groups");
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        // GET: api/UserGroup/5
        [HttpGet("{id}")]
        public async Task<ActionResult<UserGroupModel>> GetUserGroup(int id)
        {
            try
            {
                var userGroup = await _context.UserGroup.FindAsync(id);

                if (userGroup == null)
                {
                    return NotFound();
                }

                return userGroup;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserGroupController - An error occurred while retrieving the user group with ID {Id}", id);
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<UserGroupModel>>> GetUserGroupsByUserId(int userId)
        {
            try
            {
                var userGroups = await _context.UserGroup
                    .Where(ug => ug.UserID == userId)
                    .ToListAsync();

                if (userGroups == null || userGroups.Count == 0)
                {
                    return NotFound();
                }

                return userGroups;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserGroupController - An error occurred while retrieving user groups for user with ID {UserId}", userId);
                return StatusCode(500, "An error occurred while processing your request.");

            }
        }


        // POST: api/UserGroup
        [HttpPost]
        public async Task<ActionResult<UserGroupModel>> PostUserGroup(UserGroupModel userGroup)
        {
            try
            {
                if (UserAlreadyHasRecord(userGroup.UserID))
                {
                    return Conflict(new { ErrorMessage = "User already has a record in a group." });
                }

                _context.UserGroup.Add(userGroup);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetUserGroup), new { id = userGroup.UserGroupID }, userGroup);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserGroupController - An error occurred while adding a user group: {ErrorMessage}", ex.Message);
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        private bool UserAlreadyHasRecord(int userId)
        {
            return _context.UserGroup.Any(e => e.UserID == userId);
        }


        // PUT: api/UserGroup/5
        [HttpPut("{id}")]
        public async Task<ActionResult<UserGroupModel>> PutUserGroup(int id, UserGroupModel userGroup)
        {
            if (id != userGroup.UserGroupID)
            {
                return BadRequest();
            }

            _context.Entry(userGroup).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                return Ok(userGroup);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!UserGroupExists(id))
                {
                    return NotFound();
                }
                else
                {
                    _logger.LogError(ex, "UserGroupController - An error occurred while updating the user group with ID {UserGroupID}: {ErrorMessage}", id, ex.Message);
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserGroupController - An error occurred while updating the user group with ID {UserGroupID}: {ErrorMessage}", id, ex.Message);
                return StatusCode(500, "An error occurred while processing your request.");
            }


        }


        // DELETE: api/UserGroup/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUserGroup(int id)
        {
            try
            {
                var userGroup = await _context.UserGroup.FindAsync(id);
                if (userGroup == null)
                {
                    return NotFound();
                }

                _context.UserGroup.Remove(userGroup);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserGroupController - An error occurred while deleting the user group with ID {UserGroupID}: {ErrorMessage}", id, ex.Message);
                return StatusCode(500, "An error occurred while processing your request.");
            }

        }

        private bool UserGroupExists(int id)
        {
            return _context.UserGroup.Any(e => e.UserGroupID == id);
        }
    }
}
