﻿using Microsoft.AspNetCore.Identity;

namespace WebApi.Model
{
    public class ApplicationUser: IdentityUser
    {
        public string? RefreshToken { get; set; }
        public DateTime RefreshTokenExpiryTime { get; set; }
        
    }
}
