using System.ComponentModel.DataAnnotations;

namespace WebApi.Model
{
    public class UserModel
    {
        [Key]
        public int UserID { get; set; }
//[Column("UserFirstName")]
        public string Username { get; set; }
        public string UserEmail { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
        public int StoreID { get; set; }

    }

}
