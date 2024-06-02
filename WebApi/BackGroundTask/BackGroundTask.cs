using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Net.Mail;
using System.Net;
using WebApi.DAL;
using WebApi.Model;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Threading;
using System.Threading.Tasks;
using System;
using Microsoft.EntityFrameworkCore.Internal;

namespace WebApi.BackGroundTask
{
    public class BackgroundTask : IHostedService, IDisposable
    {
        private Timer _timer;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly EmailModel _configuration;
        private readonly ILogger<BackgroundTask> _logger;
        public BackgroundTask(IOptions<EmailModel> configuration, IServiceScopeFactory serviceScopeFactory, ILogger<BackgroundTask> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _configuration = configuration.Value;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("Background task started.");
            // Schedule the task to run every minute
            _timer = new Timer(TimerCallback, null, TimeSpan.Zero, TimeSpan.FromHours(12));
            return Task.CompletedTask;
        }

        private void TimerCallback(object state)
        {
            _ = DoDailyTaskAsync(state);
        }

        private async Task DoDailyTaskAsync(object state)
        {
            try
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var users = await context.Users.ToListAsync();

                    foreach (var user in users)
                    {
                        Console.WriteLine($"Called user: {user.Username}");
                        await CourseProgress(user);
                        await QuizProgress(user);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"BackgroundTask - An error occurred in the daily task: {ex.Message}");
                Console.WriteLine($"An error occurred in the daily task: {ex.Message}");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("Background task is stopping.");
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
        public async Task CourseProgress(UserModel user)
        {
            try
            {   
                // Check if the user has started any courses
                var incompleteCourses = await GetIncompleteCourses(user.UserID);

                // If the user hasn't started any courses, no need to send an email
                if (incompleteCourses.Count == 0)
                {
                    Console.WriteLine($"User {user.Username} has not started any courses yet.");
                    return;
                }

                foreach (var course in incompleteCourses)
                {
                    var courseStartDate = course.StartTime;
                    var courseId = course.CourseId;

                    var deadlineDate = courseStartDate.AddHours(10);

                    // Check if the current date has passed the course end date
                    if (DateTime.UtcNow > deadlineDate)
                    {
                        Console.WriteLine($"Sending email to {user.Username} for course with ID {courseId}: Reminder - Complete the course before the deadline.");
                        await SendEmailReminderCourse(user, courseId, deadlineDate, "course");
                    }
                    else
                    {
                        Console.WriteLine($"User {user.Username} has sufficient time to complete course with ID {courseId}.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"BackgroundTask - Error processing user {user.Username} for course progress: {ex.Message}");
                Console.WriteLine($"Error processing user {user.Username} for course progress: {ex.Message}");
            }
        }

        public async Task<List<(int CourseId, DateTime StartTime)>> GetIncompleteCourses(int userId)
        {
            try
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    // Query the database to get all incomplete courses for the user
                    var incompleteCourses = await context.UserCourse
                        .Where(uc => uc.UserID == userId && uc.StartTime == uc.EndTime) // Filter incomplete courses
                        .Select(uc => new { uc.CourseID, uc.StartTime }) // Select course ID, start time, and end time
                        .ToListAsync();

                    // Convert the result to the desired format
                    var incompleteCoursesData = incompleteCourses
                        .Select(uc => (uc.CourseID, uc.StartTime))
                        .ToList();

                    return incompleteCoursesData;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"BackgroundTask - Error retrieving incomplete courses for user with ID {userId}: {ex.Message}");
                Console.WriteLine($"Error retrieving incomplete courses for user with ID {userId}: {ex.Message}");
                // Return an empty list if an error occurs
                return new List<(int, DateTime)>();
            }
        }



        public async Task QuizProgress(UserModel user)
        {
            try
            {
                // Check if the user has started any quizzes
                var incompleteQuizzes = await GetIncompleteQuizzes(user.UserID);

                // If the user hasn't started any quizzes, no need to send an email
                if (incompleteQuizzes.Count == 0)
                {
                    Console.WriteLine($"User {user.Username} has not started any quizzes yet.");
                    return;
                }

                foreach (var quiz in incompleteQuizzes)
                {
                    var quizStartDate = quiz.StartTime;
                    var quizId = quiz.QuizId;

                    var deadlineDate = quizStartDate.AddHours(10);

                    // Check if the current date has passed the deadline
                    if (DateTime.UtcNow > deadlineDate)
                    {
                        Console.WriteLine($"Sending email to {user.Username} for quiz with ID {quizId}: Reminder - Complete the quiz before the deadline.");
                        await SendEmailReminderQuiz(user, quizId, deadlineDate, "quiz");
                    }
                    else
                    {
                        Console.WriteLine($"User {user.Username} has sufficient time to complete quiz with ID {quizId}.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"BackgroundTask - Error processing user {user.Username}: {ex.Message}");
                Console.WriteLine($"Error processing user {user.Username}: {ex.Message}");
            }
        }

        public async Task SendEmailReminderQuiz(UserModel user, int quizId, DateTime deadlineDate, string activityType)
        {
            try
            {
                string recipientEmail = user.UserEmail;
                string username = user.Username;
                string bodyTemplate = _configuration.QuizReminderEmailBodyTemplate;
                bodyTemplate = bodyTemplate.Replace("{RecipientEmail}", recipientEmail)
                    .Replace("{ActivityType}", activityType)
                    .Replace("{username}", username)
                                           .Replace("{QuizId}", quizId.ToString())
                                           .Replace("{DeadlineDate}", deadlineDate.ToString("yyyy-MM-dd"));

                using (SmtpClient client = new SmtpClient(_configuration.SmtpClient, _configuration.SmtpPort))
                {
                    client.Credentials = new NetworkCredential(_configuration.SenderEmail, _configuration.SenderPassword);
                    client.EnableSsl = true;

                    MailMessage mailMessage = new MailMessage
                    {
                        From = new MailAddress(_configuration.SenderEmail),
                        Subject = _configuration.QuizReminderEmailSubject,
                        Body = bodyTemplate,
                        IsBodyHtml = true,
                    };

                    mailMessage.To.Add(recipientEmail);

                    await client.SendMailAsync(mailMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("BackgroundTask - An error occurred while sending reminder email.", ex);
                // Log the exception and rethrow it to handle it at a higher level if needed
                throw new Exception("An error occurred while sending reminder email.", ex);
            }
        }

        public async Task SendEmailReminderCourse(UserModel user, int courseId, DateTime deadlineDate, string activityType)
        {
            try
            {
                string recipientEmail = user.UserEmail;
                string username = user.Username;
                string bodyTemplate = _configuration.QuizReminderEmailBodyTemplate;
                bodyTemplate = bodyTemplate.Replace("{RecipientEmail}", recipientEmail)
                    .Replace("{ActivityType}", activityType)
                    .Replace("{username}", username)
                    .Replace("{CourseId}", courseId.ToString())
                    .Replace("{DeadlineDate}", deadlineDate.ToString("yyyy-MM-dd"));

                using (SmtpClient client = new SmtpClient(_configuration.SmtpClient, _configuration.SmtpPort))
                {
                    client.Credentials = new NetworkCredential(_configuration.SenderEmail, _configuration.SenderPassword);
                    client.EnableSsl = true;

                    MailMessage mailMessage = new MailMessage
                    {
                        From = new MailAddress(_configuration.SenderEmail),
                        Subject = _configuration.CourseReminderEmailSubject,
                        Body = bodyTemplate,
                        IsBodyHtml = true,
                    };

                    mailMessage.To.Add(recipientEmail);

                    await client.SendMailAsync(mailMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("BackgroundTask - An error occurred while sending reminder email.", ex);
                // Log the exception and rethrow it to handle it at a higher level if needed
                throw new Exception("An error occurred while sending reminder email.", ex);
            }
        }


        public async Task<List<(int QuizId, DateTime StartTime)>> GetIncompleteQuizzes(int userId)
        {
            try
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    // Query the database to get all incomplete quizzes for the user
                    var incompleteQuizzes = await context.UserQuiz
                        .Where(uq => uq.UserID == userId && uq.StartTime == uq.EndTime)
                        .Select(uq => new { uq.QuizID, uq.StartTime })
                        .ToListAsync();

                    // Convert the result to the desired tuple format
                    var incompleteQuizzesData = incompleteQuizzes
                        .Select(uq => (uq.QuizID, uq.StartTime))
                        .ToList();

                    return incompleteQuizzesData;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"BackgroundTask - Error retrieving incomplete quizzes for user with ID {userId}: {ex.Message}");
                Console.WriteLine($"Error retrieving incomplete quizzes for user with ID {userId}: {ex.Message}");
                // Return an empty list if an error occurs
                return new List<(int, DateTime)>();
            }
        }

    }
}
