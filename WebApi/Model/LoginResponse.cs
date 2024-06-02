namespace WebApi.Model
{
    public class LoginResponse
    {
        public string token { get; set; }
        public string role { get; set; }
        public string userName { get; set; }

        public int userID { get; set; }
    }
}

