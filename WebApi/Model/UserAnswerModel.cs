using System.ComponentModel.DataAnnotations;

namespace WebApi.Model
{
    public class UserAnswerModel
    {
        [Key]
        public int UserAnswerID { get; set; }
        public int UserQuizID { get; set; }
        public int QuestionID { get; set; }
        public int SelectedOption { get; set; }
        public int CorrectOption { get; set; }
        public bool IsCorrect { get; set; }
    }
}
