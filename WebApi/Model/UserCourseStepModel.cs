using System.ComponentModel.DataAnnotations;

namespace WebApi.Model
{
    public class UserCourseStepModel
    {
        [Key]
        public int CourseStepID { get; set; }
        public int UserID { get; set; }
        public int CourseID { get; set; }
        public int StepNumber { get; set; }
        public string Status { get; set; }
    }
}
