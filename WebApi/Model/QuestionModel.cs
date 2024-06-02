using System.ComponentModel.DataAnnotations;

namespace WebApi.Model
{
    public class QuestionModel
    {
        [Key]
        public int Id { get; set; }
        public int QuizID { get; set; }
        public int QuestionNo { get; set; }
        public string QuestionText { get; set; }
        public string ImageName { get; set; }
        public string Option1 { get; set; }
        public string Option2 { get; set; }
        public string Option3 { get; set; }
        public string Option4 { get; set; }
        public int CorrectOption { get; set; }
    }
}
