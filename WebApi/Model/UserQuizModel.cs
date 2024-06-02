using System.ComponentModel.DataAnnotations;

namespace WebApi.Model
{
    public class UserQuizModel
    {
        [Key]
        public int UserQuizID { get; set; }
        public int UserID { get; set; }
        public int QuizID { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int Score { get; set; }
    }
}
