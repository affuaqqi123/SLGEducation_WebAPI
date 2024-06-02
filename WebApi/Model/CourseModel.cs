using System.ComponentModel.DataAnnotations;

namespace WebApi.Model
{
    public class CourseModel
    {
        [Key]
        public int CourseID { get; set; }        
        public string CourseName { get; set; }
        public string Description { get; set; }
        public string GroupName { get; set; }
    }

}
