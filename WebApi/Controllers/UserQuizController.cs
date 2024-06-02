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
    public class UserQuizController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<UserQuizController> _logger;

        public UserQuizController(AppDbContext context, ILogger<UserQuizController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/UserQuiz
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserQuizModel>>> GetUserQuizzes()
        {
            try
            {
                var userQuizzes = await _context.UserQuiz.ToListAsync();
                return Ok(userQuizzes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserQuizController - An error occurred while retrieving user quizzes: {ErrorMessage}", ex.Message);
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        // GET: api/UserQuiz/5
        [HttpGet("{id}")]
        public async Task<ActionResult<UserQuizModel>> GetUserQuiz(int id)
        {
            try
            {
                var userQuiz = await _context.UserQuiz.FindAsync(id);

                if (userQuiz == null)
                {
                    return NotFound();
                }

                return userQuiz;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserQuizController - An error occurred while retrieving user quiz with ID {QuizId}: {ErrorMessage}", id, ex.Message);
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        // POST: api/UserQuiz
        [HttpPost]
        public async Task<ActionResult<UserQuizModel>> PostUserQuiz(UserQuizModel userQuiz)
        {
            try
            {
                var existingRecord = await _context.UserQuiz.FirstOrDefaultAsync(uq => uq.UserID == userQuiz.UserID && uq.QuizID == userQuiz.QuizID);

                if (existingRecord != null)
                {
                    var userAnswers = await _context.UserAnswer.Where(ua => ua.UserQuizID == existingRecord.UserQuizID).ToListAsync();
                    foreach (var answer in userAnswers)
                    {
                        answer.SelectedOption = 0;
                    }
                    await _context.SaveChangesAsync();

                    return existingRecord;
                }
                else
                {
                    _context.UserQuiz.Add(userQuiz);
                    await _context.SaveChangesAsync();

                    return CreatedAtAction(nameof(GetUserQuiz), new { id = userQuiz.UserQuizID }, userQuiz);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserQuizController - An error occurred while processing the request to post a user quiz: {ErrorMessage}", ex.Message);
                return StatusCode(500, "An error occurred while processing your request.");

            }
        }

        // PUT: api/UserQuiz/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUserQuiz(int id, UserQuizModel userQuiz)
        {
            if (id != userQuiz.UserQuizID)
            {
                return BadRequest();
            }

            _context.Entry(userQuiz).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!UserQuizExists(id))
                {
                    _logger.LogWarning($"UserQuizController - User quiz with ID '{id}' not found.");
                    return NotFound();
                }
                else
                {
                    _logger.LogError(ex, "UserQuizController - An error occurred while updating the user quiz.");
                    throw;
                }
            }

            
        }

        // DELETE: api/UserQuiz/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUserQuiz(int id)
        {
            var userQuiz = await _context.UserQuiz.FindAsync(id);
            if (userQuiz == null)
            {
                _logger.LogWarning($"UserQuizController - User quiz with ID '{id}' not found.");
                return NotFound();
            }
            try
            {
                _context.UserQuiz.Remove(userQuiz);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"UserQuizController - An error occurred while deleting user quiz with ID '{id}'.");
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        private bool UserQuizExists(int id)
        {
            return _context.UserQuiz.Any(e => e.UserQuizID == id);
        }
    }
}
