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
    public class UserAnswerController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<UserAnswerController> _logger;

        public UserAnswerController(AppDbContext context, ILogger<UserAnswerController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/UserAnswer
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserAnswerModel>>> GetUserAnswers()
        {
            try
            {
                var userAnswers = await _context.UserAnswer.ToListAsync();
                return Ok(userAnswers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserAnswerController - An error occurred while retrieving user answers.");
                return StatusCode(500, "UserAnswerController - Internal server error");
            }
        }

        // GET: api/UserAnswer/5
        [HttpGet("{id}")]
        public async Task<ActionResult<UserAnswerModel>> GetUserAnswer(int id)
        {
            try
            {
                var userAnswer = await _context.UserAnswer.FindAsync(id);
                if (userAnswer == null)
                {
                    _logger.LogWarning($"UserAnswerController - User answer with ID '{id}' not found.");
                    return NotFound();
                }

                return userAnswer;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"UserAnswerController - An error occurred while retrieving user answer with ID '{id}'.");
                return StatusCode(500, "UserAnswerController - Internal server error");
            }
        }

        [HttpGet("userQuizID/{userQuizID}")]
        public async Task<ActionResult<IEnumerable<UserAnswerModel>>> GetUserAnswersByUserQuizID(int userQuizID)
        {
            try
            {
                var userAnswers = await _context.UserAnswer.Where(u => u.UserQuizID == userQuizID).ToListAsync();
                if (userAnswers == null || userAnswers.Count == 0)
                {
                    _logger.LogWarning($"UserAnswerController - No user answers found for UserQuizID '{userQuizID}'.");
                    return NotFound();
                }
                return userAnswers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"UserAnswerController - An error occurred while retrieving user answers for UserQuizID '{userQuizID}'.");
                return StatusCode(500, "UserAnswerController - Internal server error");
            }
        }

        // POST: api/UserAnswer
        [HttpPost]
        public async Task<ActionResult<UserAnswerModel>> PostUserAnswer(UserAnswerModel userAnswer)
        {
            try
            {
                var existingRecord = await _context.UserAnswer.FirstOrDefaultAsync(
                    ua => ua.UserQuizID == userAnswer.UserQuizID && ua.QuestionID == userAnswer.QuestionID);

                if (existingRecord != null)
                {
                    existingRecord.SelectedOption = userAnswer.SelectedOption;
                    existingRecord.CorrectOption = userAnswer.CorrectOption;
                    existingRecord.IsCorrect = userAnswer.IsCorrect;

                    await _context.SaveChangesAsync();

                    return Ok(existingRecord);
                }
                else
                {
                    _context.UserAnswer.Add(userAnswer);
                    await _context.SaveChangesAsync();

                    return CreatedAtAction(nameof(GetUserAnswer), new { id = userAnswer.UserAnswerID }, userAnswer); // Return 201 Created with new record
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserAnswerController - An error occurred while processing user answer.");
                return StatusCode(500, "Internal server error");
            }
        }


        // PUT: api/UserAnswer/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUserAnswer(int id, UserAnswerModel userAnswer)
        {
            if (id != userAnswer.UserAnswerID)
            {
                return BadRequest();
            }

            try
            {
                _context.Entry(userAnswer).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return NoContent();
            }

            catch (DbUpdateConcurrencyException)
            {
                if (!UserAnswerExists(id))
                {
                    _logger.LogWarning($"UserAnswerController - User answer with ID '{id}' not found.");
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"UserAnswerController - An error occurred while updating user answer with ID '{id}'.");
                return StatusCode(500, "Internal server error");
            }


        }

        // DELETE: api/UserAnswer/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUserAnswer(int id)
        {
            try
            {
                var userAnswer = await _context.UserAnswer.FindAsync(id);
                if (userAnswer == null)
                {
                    _logger.LogWarning($"UserAnswerController - User answer with ID '{id}' not found.");
                    return NotFound();
                }

                _context.UserAnswer.Remove(userAnswer);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"UserAnswerController - An error occurred while deleting user answer with ID '{id}'.");
                return StatusCode(500, "Internal server error");
            }
        }

        private bool UserAnswerExists(int id)
        {
            return _context.UserAnswer.Any(e => e.UserAnswerID == id);
        }

        // DELETE: api/UserAnswer/userQuizID/5
        [HttpDelete("userQuizID/{userQuizID}")]
        public async Task<IActionResult> DeleteUserAnswersByUserQuizID(int userQuizID)
        {
            try
            {
                var userAnswers = await _context.UserAnswer.Where(u => u.UserQuizID == userQuizID).ToListAsync();
                if (userAnswers == null || !userAnswers.Any())
                {
                    _logger.LogWarning($"UserAnswerController - No user answers found for the given UserQuizID '{userQuizID}'.");
                    return Ok("No user answers found for the given UserQuizID.");
                }

                _context.UserAnswer.RemoveRange(userAnswers);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"UserAnswerController - Deleted {userAnswers.Count} user answers for UserQuizID '{userQuizID}'.");
                return Ok("User answers deleted successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"UserAnswerController - An error occurred while deleting user answers for UserQuizID '{userQuizID}'.");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{userQuizID}/{questionID}")]
        public async Task<ActionResult<int>> GetSelectedOption(int userQuizID, int questionID)
        {
            try
            {
                var userAnswer = await _context.UserAnswer.FirstOrDefaultAsync(ua => ua.UserQuizID == userQuizID && ua.QuestionID == questionID);

                if (userAnswer == null)
                {
                    _logger.LogWarning($"UserAnswerController - No user answer found for UserQuizID '{userQuizID}' and QuestionID '{questionID}'.");
                    return Ok(null);
                }

                _logger.LogInformation($"UserAnswerController - Selected option retrieved successfully for UserQuizID '{userQuizID}' and QuestionID '{questionID}'.");
                return Ok(userAnswer.SelectedOption);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"UserAnswerController - An error occurred while retrieving selected option for UserQuizID '{userQuizID}' and QuestionID '{questionID}'.");
                return StatusCode(500, "Internal server error");
            }
        }

    }
}
