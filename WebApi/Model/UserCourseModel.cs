using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace WebApi.Model
{
    public class UserCourseModel
    {
        [Key]
        public int UserCourseID { get; set; }
        public int UserID { get; set; }
        public int CourseID { get; set; }
        public bool IsCourseCompleted { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }
}
