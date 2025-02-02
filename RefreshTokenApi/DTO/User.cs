﻿namespace RefreshTokenApi.DTO
{
    public class User
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Password { get; set; } 
        public string AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? TokenExpiryDate { get; set; }

    }
}
