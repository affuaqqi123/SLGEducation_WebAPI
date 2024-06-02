using System.ComponentModel.DataAnnotations;

namespace WebApi.Model
{
    public class GroupCourseModel
    {
        [Key]
        public int GroupCourseID { get; set; }
        public int CourseID { get; set; }
        public int GroupID { get; set; }
    }
}
