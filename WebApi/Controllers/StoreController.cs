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
    public class StoreController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<StoreController> _logger;

        public StoreController(AppDbContext context, ILogger<StoreController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/Store
        [HttpGet]
        public async Task<ActionResult<IEnumerable<StoreModel>>> GetStores()
        {
            try
            {
                var stores = await _context.Store.ToListAsync();
                _logger.LogInformation("StoreController - Successfully retrieved stores.");
                return Ok(stores);  
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "StoreController - Error occurred while retrieving stores.");
                return StatusCode(500, "StoreController - Internal server error");
            }
        }

        // GET: api/Store/1
        [HttpGet("{id}")]
        public async Task<ActionResult<StoreModel>> GetStore(int id)
        {
            try
            {
                var store = await _context.Store.FindAsync(id);

                if (store == null)
                {
                    _logger.LogWarning($"StoreController - Store with ID '{id}' not found.");
                    return NotFound();
                }

                return Ok(store);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"StoreController - An error occurred while retrieving store with ID '{id}'.");
                return StatusCode(500, "StoreController - Internal server error");
            }
        }

        // POST: api/Store
        [HttpPost]
        public async Task<ActionResult<StoreModel>> PostStore(StoreModel store)
        {
            try
            {
                _context.Store.Add(store);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"StoreController - Store with ID '{store.StoreID}' created successfully.");

                return CreatedAtAction(nameof(GetStore), new { id = store.StoreID }, store);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "StoreController - An error occurred while creating the store.");
                return StatusCode(500, "StoreController - Internal server error");
            }
        }

        // PUT: api/Store/1
        [HttpPut("{id}")]
        public async Task<IActionResult> PutStore(int id, StoreModel store)
        {
            if (id != store.StoreID)
            {
                _logger.LogError("StoreController - Invalid request: ID in the URL does not match the ID in the request body.");
                return BadRequest();
            }

            try
            {
                _context.Entry(store).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"StoreController - Store with ID '{id}' updated successfully.");
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!StoreExists(id))
                {
                    _logger.LogWarning($"StoreController - Store with ID '{id}' not found.");
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"StoreController - An error occurred while updating the store with ID '{id}'.");
                return StatusCode(500, "StoreController - Internal server error");
            }
        }

        // DELETE: api/Store/1
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStore(int id)
        {
            try
            {
                var store = await _context.Store.FindAsync(id);
                if (store == null)
                {
                    _logger.LogWarning($"StoreController - Store with ID '{id}' not found.");
                    return NotFound();
                }

                _context.Store.Remove(store);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"StoreController - Store with ID '{id}' deleted successfully.");
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"StoreController - An error occurred while deleting the store with ID '{id}'.");
                return StatusCode(500, "StoreController - Internal server error");
            }
        }

        private bool StoreExists(int id)
        {
            return _context.Store.Any(e => e.StoreID == id);
        }
    }

}
