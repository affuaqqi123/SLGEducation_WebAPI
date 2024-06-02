using Microsoft.EntityFrameworkCore;

namespace WebApi.Model
{
    [Keyless]
    public class TokenModel
    {
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
    }
}
