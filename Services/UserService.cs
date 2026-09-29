using Microsoft.EntityFrameworkCore;
using WMS.Data;
using WMS.Helpers;
using WMS.Models;

namespace WMS.Services;

public class UserService
{
    private readonly IDbContextFactory<WmsDbContext> _contextFactory;

    public UserService(IDbContextFactory<WmsDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<User>> GetAllAsync(string? search = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(u => u.Login.ToLower().Contains(s) || u.FullName.ToLower().Contains(s));
        }

        return await query.OrderBy(u => u.Login).ToListAsync();
    }

    public async Task<(bool Success, string? Error)> AddAsync(string login, string password, string fullName, UserRole role)
    {
        var validationError = Validate(login, fullName);
        if (validationError != null)
        {
            return (false, validationError);
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            return (false, "Пароль обязателен для нового пользователя.");
        }

        await using var context = await _contextFactory.CreateDbContextAsync();

        if (await context.Users.AnyAsync(u => u.Login == login.Trim()))
        {
            return (false, "Пользователь с таким логином уже существует.");
        }

        context.Users.Add(new User
        {
            Login = login.Trim(),
            PasswordHash = PasswordHasher.Hash(password),
            FullName = fullName.Trim(),
            Role = role
        });

        await context.SaveChangesAsync();
        return (true, null);
    }

    /// Если password пустой - пароль не меняется, обновляются только логин/имя/роль
    public async Task<(bool Success, string? Error)> UpdateAsync(int id, string login, string? password, string fullName, UserRole role)
    {
        var validationError = Validate(login, fullName);
        if (validationError != null)
        {
            return (false, validationError);
        }

        await using var context = await _contextFactory.CreateDbContextAsync();

        if (await context.Users.AnyAsync(u => u.Login == login.Trim() && u.Id != id))
        {
            return (false, "Пользователь с таким логином уже существует.");
        }

        var existing = await context.Users.FindAsync(id);
        if (existing == null)
        {
            return (false, "Пользователь не найден.");
        }

        existing.Login = login.Trim();
        existing.FullName = fullName.Trim();
        existing.Role = role;

        if (!string.IsNullOrWhiteSpace(password))
        {
            existing.PasswordHash = PasswordHasher.Hash(password);
        }

        await context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id)
    {
        if (CurrentSession.CurrentUser?.Id == id)
        {
            return (false, "Нельзя удалить пользователя, под которым вы сейчас работаете.");
        }

        await using var context = await _contextFactory.CreateDbContextAsync();
        var user = await context.Users.FindAsync(id);
        if (user == null)
        {
            return (false, "Пользователь не найден.");
        }

        context.Users.Remove(user);

        try
        {
            await context.SaveChangesAsync();
            return (true, null);
        }
        catch (DbUpdateException)
        {
            return (false, "Невозможно удалить пользователя: на него есть ссылки в системе.");
        }
    }

    private static string? Validate(string login, string fullName)
    {
        if (string.IsNullOrWhiteSpace(login))
        {
            return "Логин обязателен для заполнения.";
        }

        if (string.IsNullOrWhiteSpace(fullName))
        {
            return "ФИО обязательно для заполнения.";
        }

        return null;
    }
}