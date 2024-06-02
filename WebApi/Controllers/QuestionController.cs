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

    public class QuestionController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly ILogger<QuestionController> _logger;

        public QuestionController(AppDbContext context, IConfiguration config, ILogger<QuestionController> logger)
        {
            _context = context;
            _config = config;
            _logger = logger;

        }

        // GET: api/Question
        [HttpGet]
        public async Task<ActionResult<IEnumerable<QuestionModel>>> GetQuestions()
        {
            try
            {
                var questions = await _context.Question.ToListAsync();

                _logger.LogInformation("QuestionController - Questions retrieved successfully.");

                return questions;
            }
            catch (Exception ex)
            {
                _logger.LogError($"QuestionController - Error occurred while retrieving questions: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }


        // GET: api/Question/5
        [HttpGet("{id}")]
        public async Task<ActionResult<QuestionModel>> GetQuestion(int id)
        {
            try
            {
                var question = await _context.Question.FindAsync(id);

                if (question == null)
                {
                    _logger.LogWarning($"QuestionController - Question with ID '{id}' not found.");
                    return NotFound();
                }

                _logger.LogInformation($"QuestionController - Question with ID '{id}' retrieved successfully.");

                return question;
            }
            catch (Exception ex)
            {
                _logger.LogError($"QuestionController - Error occurred while retrieving question with ID '{id}': {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }


        // GET: api/Question/QuizID/5
        [HttpGet("QuizID/{quizid}")]
        public async Task<ActionResult<IEnumerable<QuestionModel>>> GetQuestionsByQuizID(int quizid)
        {
            try
            {
                var questions = await _context.Question.Where(q => q.QuizID == quizid).ToListAsync();

                if (questions == null || questions.Count == 0)
                {
                    _logger.LogWarning($"QuestionController - No questions found for quiz ID '{quizid}'.");
                    return Ok("No Question is added for this Quiz");
                }

                _logger.LogInformation($"QuestionController - Questions for quiz ID '{quizid}' retrieved successfully.");

                return Ok(questions);
            }
            catch (Exception ex)
            {
                _logger.LogError($"QuestionController - Error occurred while retrieving questions for quiz ID '{quizid}': {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }



        // POST: api/Question
        [HttpPost]
        public async Task<ActionResult<QuestionModel>> PostQuestion([FromForm] QuestionModel question, IFormFile ImageFile = null)
        {
            try
            {
                if (ImageFile != null)
                {
                    //var basePath = Directory.GetCurrentDirectory();
                    var basePath = Path.Combine(_config["AssetFolder:AssetFolderPath"]);
                    var quizFolderPath = Path.Combine(basePath, $"Quiz_{question.QuizID}", $"Question_{question.QuestionNo}");
                    Directory.CreateDirectory(quizFolderPath); // Create directory if it doesn't exist

                    var imageName = $"{Guid.NewGuid()}{Path.GetExtension(ImageFile.FileName)}";
                    var imagePath = Path.Combine(quizFolderPath, imageName);

                    using (var stream = new FileStream(imagePath, FileMode.Create))
                    {
                        await ImageFile.CopyToAsync(stream);
                    }

                    question.ImageName = imageName;
                }

                _context.Question.Add(question);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"QuestionController - Question with ID '{question.Id}' created successfully.");

                return CreatedAtAction(nameof(GetQuestion), new { id = question.Id }, question);
            }
            catch (Exception ex)
            {
                _logger.LogError($"QuestionController - Error occurred while creating question: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }


        // PUT: api/Question/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutQuestion(int id, [FromForm] QuestionModel question, IFormFile ImageFile = null)
        {
            try
            {
                if (id != question.Id)
                {
                    return BadRequest();
                }

                var existingQuestion = await _context.Question.FindAsync(id);

                existingQuestion.ImageName = question.ImageName;

                if (existingQuestion == null)
                {
                    _logger.LogWarning($"QuestionController - Question with ID '{id}' not found.");
                    return NotFound();
                }

                if (ImageFile != null)
                {
                    var basePath = Path.Combine(_config["AssetFolder:AssetFolderPath"]);
                    var quizFolderPath = Path.Combine(basePath, $"Quiz_{existingQuestion.QuizID}", $"Question_{existingQuestion.QuestionNo}");

                    var imageName = $"{Guid.NewGuid()}{Path.GetExtension(ImageFile.FileName)}";
                    var imagePath = Path.Combine(quizFolderPath, imageName);

                    // Delete existing image if it exists
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                    else
                    {
                        Directory.CreateDirectory(quizFolderPath);
                    }

                    // Save new image
                    using (var stream = new FileStream(imagePath, FileMode.Create))
                    {
                        await ImageFile.CopyToAsync(stream);
                    }

                    existingQuestion.ImageName = imageName;
                }

                existingQuestion.QuestionText = question.QuestionText;
                existingQuestion.Option1 = question.Option1;
                existingQuestion.Option2 = question.Option2;
                existingQuestion.Option3 = question.Option3;
                existingQuestion.Option4 = question.Option4;
                existingQuestion.CorrectOption = question.CorrectOption;

                _context.Entry(existingQuestion).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!QuestionExists(id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                _logger.LogInformation($"QuestionController - Question with ID '{id}' updated successfully.");
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError($"QuestionController - Error occurred while updating question with ID '{id}': {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");

            }
        }


        [HttpGet("Image/{quizId}/{questionNo}/{imageName}")]
        public IActionResult GetQuestionImage(int quizId, int questionNo, string imageName)
        {
            try
            {
                var basePath = Path.Combine(_config["AssetFolder:AssetFolderPath"], $"Quiz_{quizId}", $"Question_{questionNo}");
                var imagePath = Path.Combine(basePath, imageName);

                if (!System.IO.File.Exists(imagePath))
                {
                    _logger.LogWarning($"Image '{imageName}' for Quiz ID '{quizId}' and Question No '{questionNo}' not found.");
                    return NotFound();
                }

                // Return the image file
                var imageBytes = System.IO.File.ReadAllBytes(imagePath);
                return File(imageBytes, "image/jpeg");
            }
            catch (Exception ex)
            {
                _logger.LogError($"QuestionController - Error occurred while retrieving question image: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }

        // DELETE: api/Question/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteQuestion(int id)
        {
            try
            {
                var question = await _context.Question.FindAsync(id);
                if (question == null)
                {
                    _logger.LogWarning($"QuestionController - Question with ID '{id}' not found.");
                    return NotFound();
                }

                _context.Question.Remove(question);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"QuestionController - Question with ID '{id}' deleted successfully.");
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError($"QuestionController - Error occurred while deleting question with ID '{id}': {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");

            }
        }

        private bool QuestionExists(int id)
        {
            try
            {
                return _context.Question.Any(e => e.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError($"QuestionController - Error occurred while checking if question with ID '{id}' exists: {ex.Message}");
                throw; // Rethrow the exception to propagate it further if needed
            }
        }
    }
}
