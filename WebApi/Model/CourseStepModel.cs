using System.ComponentModel.DataAnnotations;

namespace WebApi.Model
{
    public class CourseStepModel
    {
        [Key]
        public int ID { get; set; }
        public int CourseID { get; set; }
        public int StepNo { get; set; }
        public string StepTitle { get; set; }
        public string StepContent { get; set; }
        public string ContentType { get; set; }
        public string Description { get; set; }
    }
}
