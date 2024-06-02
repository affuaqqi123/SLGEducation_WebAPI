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

    public class QuizController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<QuizController> _logger;

        public QuizController(AppDbContext context, ILogger<QuizController> logger)
        {
            _context = context;
            _logger = logger;

        }
        [HttpGet]
        public async Task<ActionResult<IEnumerable<QuizModel>>> GetQuizzes()
        {
            try
            {
                var quizzes = await _context.Quiz.ToListAsync();
                if (quizzes.Count == 0)
                {
                    return Ok("No quizzes found.");
                }
                return Ok(quizzes);
            }
            catch (Exception ex)
            {
                _logger.LogError($"QuizController - Error occurred while retrieving quizzes: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }


        // GET: api/Quiz/5
        [HttpGet("{id}")]
        public async Task<ActionResult<QuizModel>> GetQuiz(int id)
        {
            try
            {
                var quiz = await _context.Quiz.FindAsync(id);

                if (quiz == null)
                {
                    _logger.LogWarning($"QuizController - Quiz with ID '{id}' not found.");
                    return NotFound();
                }

                return quiz;
            }
            catch (Exception ex)
            {
                _logger.LogError($"QuizController - Error occurred while retrieving quiz with ID '{id}': {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }

        // POST: api/Quiz
        [HttpPost]
        public async Task<ActionResult<QuizModel>> PostQuiz(QuizModel quiz)
        {
            try
            {
                _context.Quiz.Add(quiz);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"QuizController - Quiz with ID '{quiz.QuizID}' created successfully.");


                return CreatedAtAction(nameof(GetQuiz), new { id = quiz.QuizID }, quiz);
            }
            catch (Exception ex)
            {
                _logger.LogError($"QuizController - Error occurred while creating quiz: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }

        // PUT: api/Quiz/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutQuiz(int id, QuizModel quiz)
        {
            if (id != quiz.QuizID)
            {
                return BadRequest();
            }
            try
            {
                _context.Entry(quiz).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"QuizController - Quiz with ID '{id}' updated successfully.");
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!QuizExists(id))
                {
                    _logger.LogWarning($"QuizController - Quiz with ID '{id}' not found.");
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"QuizController - Error occurred while updating quiz with ID '{id}': {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }


        }

        // DELETE: api/Quiz/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteQuiz(int id)
        {
            try
            {
                var quiz = await _context.Quiz.FindAsync(id);
                if (quiz == null)
                {
                    _logger.LogWarning($"QuizController - Quiz with ID '{id}' not found.");
                    return NotFound();
                }

                _context.Quiz.Remove(quiz);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"QuizController - Quiz with ID '{id}' deleted successfully.");
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError($"QuizController - Error occurred while deleting quiz with ID '{id}': {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }

        // GET: api/Quiz/ByCourse/{courseId}
        [HttpGet("ByCourse/{courseId}")]
        public async Task<ActionResult<QuizModel>> GetQuizByCourse(int courseId)
        {
            try
            {
                var quiz = await _context.Quiz.FirstOrDefaultAsync(q => q.CourseID == courseId);

                if (quiz == null)
                {
                    _logger.LogWarning($"QuizController - Quiz for course with ID '{courseId}' not found.");
                    return NotFound();
                }

                return quiz;
            }
            catch (Exception ex)
            {
                _logger.LogError($"QuizController - Error occurred while retrieving quiz for course with ID '{courseId}': {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }

        public class QuizCompletionResponse
        {
            public bool IsCompleted { get; set; }
            public string ErrorMessage { get; set; }
        }

        // GET: api/UserQuiz/IsQuizCompleted
        [HttpGet("IsQuizCompleted")]
        public async Task<ActionResult<QuizCompletionResponse>> IsQuizCompleted(int userId, int courseId)
        {
            try
            {
                // Find the quiz associated with the provided course ID
                var quiz = await _context.Quiz.FirstOrDefaultAsync(q => q.CourseID == courseId);

                if (quiz == null)
                {
                    // Quiz not found for the given course ID
                    return new QuizCompletionResponse
                    {
                        IsCompleted = false,
                        ErrorMessage = "There is no Quiz for this Course"
                    };
                }

                // Check if there is a record for the user and quiz in the UserQuiz table
                var userQuiz = await _context.UserQuiz.FirstOrDefaultAsync(uq => uq.UserID == userId && uq.QuizID == quiz.QuizID);

                if (userQuiz == null || userQuiz.StartTime == userQuiz.EndTime)
                {
                    // Either there's no record for the user and quiz or start and end times are different, indicating the quiz is not completed
                    return new QuizCompletionResponse
                    {
                        IsCompleted = false
                    };
                }

                // Start and end times are the same, indicating the quiz is completed
                return new QuizCompletionResponse
                {
                    IsCompleted = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while checking quiz completion status: {ErrorMessage}", ex.Message);
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }




        private bool QuizExists(int id)
        {
            return _context.Quiz.Any(e => e.QuizID == id);
        }
    }
}
