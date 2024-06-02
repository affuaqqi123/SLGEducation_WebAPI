using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace WebApi.Model
{
    public class UserGroupModel
    {
        [Key]
        public int UserGroupID { get; set; }
        public int UserID { get; set; }
        public int GroupID { get; set; }
    }
    
}
