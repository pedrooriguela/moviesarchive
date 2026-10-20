using MoviesArchive.Domain.Common;

namespace MoviesArchive.Domain.Models.Users;

public class User : Entity
{
    public User(
        string name,
        string email)
    {
        Name = name;
        Email = email;
    }

    public string Name { get; set; }
    public string Email { get; set; }
    public string Password { get; set; } = string.Empty;
    public byte[]? PhotoData { get; set; }
    public string? PhotoContentType { get; set; }
    public Guid? PlanId { get; set; }

    public void SetPassword (string password) =>
        Password = password;

    public class Builder
    {
        public static User Create(string name, string email) =>
            new(name, email);
    }
}
