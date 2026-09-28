using Microsoft.EntityFrameworkCore;
using WMS.Data;
using WMS.Helpers;
using WMS.Models;

namespace WMS.Services;

public class AuthService
{
    private readonly IDbContextFactory<WmsDbContext> _contextFactory;

    public AuthService(IDbContextFactory<WmsDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<AuthResult> SignInAsync(string login, string password)
    {
        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
        {
            return AuthResult.Fail("Введите логин и пароль.");
        }

        await using var context = await _contextFactory.CreateDbContextAsync();
        var user = await context.Users.FirstOrDefaultAsync(u => u.Login == login);

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