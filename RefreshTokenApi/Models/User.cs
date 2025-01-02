using System;
using System.Collections.Generic;

namespace RefreshTokenApi.Models;

public partial class User
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string? RefreshToken { get; set; }

    public DateTime? TokenExpiryDate { get; set; }

    public string? AccessToken { get; set; }
}
