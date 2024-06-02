using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using System.Text.RegularExpressions;
using WebApi.Model;

namespace WebApi.DAL
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<UserModel> Users { get; set; }
        public DbSet<GroupModel> Groups { get; set; }
        public DbSet<GroupCourseModel> GroupCourses { get; set; }
        public DbSet<CourseModel> Courses { get; set; }
        public DbSet<UserCourseStepModel> UserCourseStep { get; set; }
        public DbSet<CourseStepModel> CourseStep { get; set; }
        public DbSet<UserGroupModel> UserGroup { get; set; }

        //public DbSet<AspNetUsers> ApplicationUser { get; set; }

        public DbSet<TokenModel> Token { get; set; }

        public DbSet<Response> Response { get; set; }

        public DbSet<QuizModel> Quiz { get; set; }
        public DbSet<QuestionModel> Question { get; set; }
        public DbSet<UserCourseModel> UserCourse { get; set; }
        public DbSet<UserQuizModel> UserQuiz { get; set; }
        public DbSet<UserAnswerModel> UserAnswer { get; set; }
        public DbSet<StoreModel> Store { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
