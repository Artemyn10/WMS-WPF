using Microsoft.EntityFrameworkCore;
using WMS.Data;
using WMS.Helpers;
using WMS.Models;

namespace WMS.Services;

public class AuthService
{
    private readonly WmsDbContext _context;

    public AuthService(WmsDbContext context)
    {
        _context = context;
    }

    public async Task<AuthResult> SignInAsync(string login, string password)
    {
        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
        {
            return AuthResult.Fail("Введите логин и пароль.");
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Login == login);

        if (user == null || !PasswordHasher.Verify(password, user.PasswordHash))
        {
            return AuthResult.Fail("Неверный логин или пароль.");
        }

        CurrentSession.SignIn(user);
        return AuthResult.Ok();
    }
}

public class AuthResult
{
    public bool Success { get; private init; }
    public string? ErrorMessage { get; private init; }

    public static AuthResult Ok() => new() { Success = true };
    public static AuthResult Fail(string message) => new() { Success = false, ErrorMessage = message };
}