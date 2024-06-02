using System.ComponentModel.DataAnnotations;

namespace WebApi.Model
{
    public class QuizModel
    {
        [Key]
        public int QuizID { get; set; }
        public int CourseID { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
    }
}
