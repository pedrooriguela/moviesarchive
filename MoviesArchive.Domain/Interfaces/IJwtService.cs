using System;
using System.Collections.Generic;
using System.Text;
using MoviesArchive.Domain.Models.Users;

namespace MoviesArchive.Domain.Interfaces;

public interface IJwtService
{
    string GenerateToken(User user);
    string GeneratePasswordResetToken(User user);
}
