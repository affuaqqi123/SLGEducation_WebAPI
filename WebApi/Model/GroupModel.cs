using System.ComponentModel.DataAnnotations;

namespace WebApi.Model
{
    public class GroupModel
    {
        [Key]
        public int GroupID { get; set; }
        [Required]
        public string GroupName { get; set; }
    }

}
