using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApi.DAL;
using WebApi.Model;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserCourseStepController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<UserCourseStepController> _logger;

        public UserCourseStepController(AppDbContext context, ILogger<UserCourseStepController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/CourseStep
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserCourseStepModel>>> GetUserCourseStep()
        {
            try
            {
                var groupedUserCourseSteps = await _context.UserCourseStep
                    .GroupBy(ucs => new { ucs.UserID, ucs.CourseID, ucs.StepNumber })
                    .ToListAsync();

                foreach (var group in groupedUserCourseSteps)
                {
                    // Keep the first record from each group
                    var firstRecord = group.First();

                    // Delete the other records from the database
                    foreach (var record in group.Skip(1))
                    {
                        _context.UserCourseStep.Remove(record);
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(groupedUserCourseSteps.Select(group => group.First()));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserCourseStepController - An error occurred while fetching user course steps");
                return StatusCode(500, "An error occurred while fetching user course steps");
            }
        }


        // GET: api/CourseStep/1
        [HttpGet("{id}")]
        public async Task<ActionResult<UserCourseStepModel>> GetCourseStep(int id)
        {
            try
            {
                var courseStep = await _context.UserCourseStep.FindAsync(id);

                if (courseStep == null)
                {
                    return NotFound();
                }

                return Ok(courseStep);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"UserCourseStepController - An error occurred while fetching course step with ID '{id}'");
                return StatusCode(500, "An error occurred while fetching course step");
            }
        }

        // POST: api/CourseStep
        [HttpPost]
        public async Task<ActionResult<UserCourseStepModel>> PostCourseStep(UserCourseStepModel courseStep)
        {
            try
            {
                var existingRecord = await _context.UserCourseStep
            .Where(ucs => ucs.CourseID == courseStep.CourseID && ucs.UserID == courseStep.UserID && ucs.StepNumber == courseStep.StepNumber)
            .FirstOrDefaultAsync();

                if (existingRecord != null)
                {
                    return Ok(existingRecord);
                }
                _context.UserCourseStep.Add(courseStep);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetCourseStep), new { id = courseStep.CourseStepID }, courseStep);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserCourseStepController - An error occurred while posting course step");
                return StatusCode(500, "An error occurred while posting course step");
            }
        }

        // PUT: api/CourseStep/1
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCourseStep(int id, UserCourseStepModel courseStep)
        {
            if (id != courseStep.CourseStepID)
            {
                return BadRequest();
            }

            _context.Entry(courseStep).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!CourseStepExists(id))
                {
                    _logger.LogError(ex, "UserCourseStepController - Course step not found while updating");
                    return NotFound();
                }
                else
                {
                    _logger.LogError(ex, "UserCourseStepController - Concurrency exception occurred while updating course step");
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserCourseStepController - An error occurred while updating course step");
                return StatusCode(500, "An error occurred while updating course step");
            }


        }

        // DELETE: api/CourseStep/1
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCourseStep(int id)
        {
            try
            {
                var courseStep = await _context.UserCourseStep.FindAsync(id);
                if (courseStep == null)
                {
                    return NotFound();
                }

                _context.UserCourseStep.Remove(courseStep);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"UserCourseStepController - An error occurred while deleting course step with ID '{id}'");
                return StatusCode(500, "An error occurred while deleting course step");
            }
        }


        [HttpGet("ByCourseAndUser/{courseId}/{userId}")]
        public async Task<ActionResult<IEnumerable<UserCourseStepModel>>> GetUserCourseStepsByCourseAndUser(int courseId, int userId)
        {
            try
            {
                var userCourseSteps = await _context.UserCourseStep
             .Where(ucs => ucs.CourseID == courseId && ucs.UserID == userId)
             .ToListAsync();

                if (userCourseSteps == null || !userCourseSteps.Any())
                {
                    _logger.LogWarning($"UserCourseStepController - No user course steps found for course ID '{courseId}' and user ID '{userId}'.");
                    return Ok();
                }

                var groupedUserCourseSteps = userCourseSteps
                    .GroupBy(ucs => new { ucs.UserID, ucs.CourseID, ucs.StepNumber })
                    .ToList();

                foreach (var group in groupedUserCourseSteps)
                {

                    var firstRecord = group.First();


                    userCourseSteps.RemoveAll(record => group.Contains(record) && record != firstRecord);
                }


                foreach (var group in groupedUserCourseSteps)
                {
                    foreach (var record in group.Skip(1))
                    {
                        _context.UserCourseStep.Remove(record);
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(userCourseSteps);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"UserCourseStepController - An error occurred while retrieving user course steps for course ID '{courseId}' and user ID '{userId}'");
                return StatusCode(500, "An error occurred while retrieving user course steps");
            }
        }
        private bool CourseStepExists(int id)
        {
            return _context.UserCourseStep.Any(e => e.CourseStepID == id);
        }


        [HttpPut("UpdateStatus")]

        public async Task<IActionResult> UpdateUserCourseStepStatusAndVideoTime(int courseId, int userId, int stepNumber, string status)
        {
            try
            {
                var userCourseStep = await _context.UserCourseStep
                .Where(ucs => ucs.CourseID == courseId && ucs.UserID == userId && ucs.StepNumber == stepNumber)
                .FirstOrDefaultAsync();

                if (userCourseStep == null)
                {
                    _logger.LogWarning($"UserCourseStepController - User course step with CourseID '{courseId}', UserID '{userId}', and StepNumber '{stepNumber}' not found.");
                    return Ok();
                }
                userCourseStep.Status = status;

                await _context.SaveChangesAsync();
                _logger.LogInformation($"UserCourseStepController - User course step with CourseID '{courseId}', UserID '{userId}', and StepNumber '{stepNumber}' updated successfully.");
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"UserCourseStepController - An error occurred while updating user course step with CourseID '{courseId}', UserID '{userId}', and StepNumber '{stepNumber}'");
                return StatusCode(500, "An error occurred while updating user course step");
            }
        }
        // POST: api/UserCourseStep/IsCourseCompleted
        [HttpGet("IsCourseCompleted")]
        public async Task<ActionResult<bool>> IsCourseCompleted(int userId, int courseId)
        {
            try
            {
                var userCourseSteps = await _context.UserCourseStep
                .Where(ucs => ucs.UserID == userId && ucs.CourseID == courseId)
                .ToListAsync();

                // If there are no user course steps, the course is not completed
                if (!userCourseSteps.Any())
                {
                    return Ok(false);
                }

                // Check if all steps are completed
                bool allCompleted = userCourseSteps.All(ucs => ucs.Status == "Completed");

                return Ok(allCompleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"UserCourseStepController - An error occurred while checking if the course with ID '{courseId}' is completed for user with ID '{userId}'");
                return StatusCode(500, "An error occurred while checking if the course is completed");
            }
        }

        // GET: api/UserCourseSteps/5
        [HttpGet("GetUser&CourseSteps/{courseId}")]
        public async Task<ActionResult<IEnumerable<UserCourseProgressDto>>> GetCourseSteps(int courseId)
        {
            try
            {
                var groupName = await _context.Courses
                    .Where(c => c.CourseID == courseId)
                    .Select(c => c.GroupName)
                    .FirstOrDefaultAsync();

                var groupID = await _context.Groups
                    .Where(g => g.GroupName == groupName)
                    .Select(g => g.GroupID)
                    .FirstOrDefaultAsync();

                var userIDs = await _context.UserGroup
                    .Where(ug => ug.GroupID == groupID)
                    .Select(ug => ug.UserID)
                    .ToListAsync();

                if (userIDs.Count == 0)
                {
                    return NotFound("No users assigned to this course.");
                }
                var userCourseProgressList = new List<UserCourseProgressDto>();

                foreach (var userID in userIDs)
                {
                    var userHasSteps = await _context.UserCourseStep
                        .AnyAsync(ucs => ucs.UserID == userID && ucs.CourseID == courseId);


                    if (userHasSteps)
                    {
                        var userCourseSteps = await _context.UserCourseStep
                            .Where(ucs => ucs.UserID == userID && ucs.CourseID == courseId)
                            .OrderBy(ucs => ucs.StepNumber)
                            .ToListAsync();

                        int totalSteps = await _context.CourseStep.CountAsync(cs => cs.CourseID == courseId);
                        int userCompletedSteps = userCourseSteps.Count(ucs => ucs.Status == "Completed");
                        var user = _context.Users.FirstOrDefault(u => u.UserID == userID);
                        userCourseProgressList.Add(new UserCourseProgressDto
                        {
                            UserName = user.Username,
                            StoreID = user.StoreID,
                            ProgressPercentage = Math.Round((double)userCompletedSteps / totalSteps * 100, 2),
                            Status = userCompletedSteps == totalSteps ? "Completed" : "In Progress",
                            QuizScore = await GetQuizScoreForUser(userID, courseId)
                        });

                    }
                    else
                    {
                        var user = _context.Users.FirstOrDefault(u => u.UserID == userID);
                        userCourseProgressList.Add(new UserCourseProgressDto
                        {
                            UserName = user.Username,
                            StoreID = user.StoreID,
                            ProgressPercentage = 0,
                            Status = "Not Started",
                            QuizScore = 0
                        });
                    }
                }
                return Ok(userCourseProgressList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"UserCourseStepController - An error occurred while retrieving course steps for course with ID '{courseId}'");
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        private async Task<int> GetQuizScoreForUser(int userID, int courseId)
        {
            try
            {
                var quizId = await _context.Quiz
            .Where(q => q.CourseID == courseId)
            .Select(q => q.QuizID)
            .FirstOrDefaultAsync();

                if (quizId == 0)
                {
                    return 0;
                }
                var quizScore = await _context.UserQuiz
                .Where(uq => uq.UserID == userID && uq.QuizID == quizId)
                .Select(uq => uq.Score)
                .FirstOrDefaultAsync();

                return quizScore;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while retrieving quiz score for user with ID '{userID}' and course with ID '{courseId}': {ex.Message}");
                throw;
            }
        }

        public class UserCourseProgressDto
        {
            public string UserName { get; set; }
            public int StoreID { get; set; }
            public double ProgressPercentage { get; set; }
            public string Status { get; set; }
            public int QuizScore { get; set; }
        }

    }
}