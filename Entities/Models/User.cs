using Microsoft.AspNetCore.Identity;

namespace Entities.Models;

public class User : IdentityUser
{
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string RefreshToken { get; set; } = null!;
    public DateTime RefreshTokenExpiryTime { get; set; }
}
