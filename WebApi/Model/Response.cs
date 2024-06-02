using Microsoft.EntityFrameworkCore;

namespace WebApi.Model
{
    [Keyless]
    public class Response
    {
        public string? Status { get; set; }
        public string? Message { get; set; }
    }
}
