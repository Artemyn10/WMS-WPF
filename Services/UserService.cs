using Microsoft.EntityFrameworkCore;
using WMS.Data;
using WMS.Helpers;
using WMS.Models;

namespace WMS.Services;

public class UserService
{
    private const int MinPasswordLength = 6;

    private readonly IDbContextFactory<WmsDbContext> _contextFactory;

    public UserService(IDbContextFactory<WmsDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<User>> GetAllAsync(string? search = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.Users.Include(user => user.Warehouse).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var value = search.Trim().ToLower();
            query = query.Where(user =>
                user.Login.ToLower().Contains(value) ||
                user.FullName.ToLower().Contains(value));
        }

        return await query.OrderBy(user => user.Login).ToListAsync();
    }

    public async Task<(bool Success, string? Error)> AddAsync(
        string login,
        string password,
        string fullName,
        UserRole role,
        int? warehouseId)
    {
        var validationError = Validate(login, fullName, role, warehouseId);
        if (validationError != null)
        {
            return (false, validationError);
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            return (false, "Пароль обязателен для нового пользователя.");
        }

        var passwordError = ValidatePassword(password);
        if (passwordError != null)
        {
            return (false, passwordError);
        }

        await using var context = await _contextFactory.CreateDbContextAsync();

        var normalizedLogin = login.Trim();
        if (await context.Users.AnyAsync(user => user.Login == normalizedLogin))
        {
            return (false, "Пользователь с таким логином уже существует.");
        }

        context.Users.Add(new User
        {
            Login = normalizedLogin,
            PasswordHash = PasswordHasher.Hash(password),
            FullName = fullName.Trim(),
            Role = role,
            WarehouseId = NormalizeWarehouse(role, warehouseId),
            IsWarehouseResponsible = false
        });

        await context.SaveChangesAsync();
        return (true, null);
    }

    /// <summary>Если password пустой, пароль не меняется.</summary>
    public async Task<(bool Success, string? Error)> UpdateAsync(
        int id,
        string login,
        string? password,
        string fullName,
        UserRole role,
        int? warehouseId)
    {
        var validationError = Validate(login, fullName, role, warehouseId);
        if (validationError != null)
        {
            return (false, validationError);
        }

        if (!string.IsNullOrWhiteSpace(password))
        {
            var passwordError = ValidatePassword(password);
            if (passwordError != null)
            {
                return (false, passwordError);
            }
        }

        await using var context = await _contextFactory.CreateDbContextAsync();

        var normalizedLogin = login.Trim();
        if (await context.Users.AnyAsync(user =>
                user.Login == normalizedLogin && user.Id != id))
        {
            return (false, "Пользователь с таким логином уже существует.");
        }

        var existing = await context.Users.FindAsync(id);
        if (existing == null)
        {
            return (false, "Пользователь не найден.");
        }

        if (existing.Role == UserRole.Administrator &&
            role != UserRole.Administrator)
        {
            var administrators =
                await context.Users.CountAsync(user =>
                    user.Role == UserRole.Administrator);

            if (administrators <= 1)
            {
                return (
                    false,
                    "Нельзя снять роль с последнего администратора системы.");
            }
        }

        var normalizedWarehouseId = NormalizeWarehouse(role, warehouseId);
        if (existing.WarehouseId != normalizedWarehouseId)
        {
            existing.IsWarehouseResponsible = false;
        }

        existing.Login = normalizedLogin;
        existing.FullName = fullName.Trim();
        existing.Role = role;
        existing.WarehouseId = normalizedWarehouseId;

        if (!string.IsNullOrWhiteSpace(password))
        {
            existing.PasswordHash = PasswordHasher.Hash(password);
        }

        await context.SaveChangesAsync();
        return (true, null);
    }

    /// <summary>Для смены собственного пароля требуется знать текущий пароль.</summary>
    public async Task<(bool Success, string? Error)> ChangePasswordAsync(
        int userId,
        string currentPassword,
        string newPassword,
        string confirmPassword)
    {
        if (string.IsNullOrEmpty(currentPassword))
        {
            return (false, "Введите текущий пароль.");
        }

        var passwordError = ValidatePassword(newPassword);
        if (passwordError != null)
        {
            return (false, passwordError);
        }

        if (newPassword != confirmPassword)
        {
            return (false, "Новый пароль и подтверждение не совпадают.");
        }

        if (newPassword == currentPassword)
        {
            return (false, "Новый пароль должен отличаться от текущего.");
        }

        await using var context = await _contextFactory.CreateDbContextAsync();

        var user = await context.Users.FindAsync(userId);
        if (user == null)
        {
            return (false, "Пользователь не найден.");
        }

        if (!PasswordHasher.Verify(currentPassword, user.PasswordHash))
        {
            return (false, "Текущий пароль указан неверно.");
        }

        user.PasswordHash = PasswordHasher.Hash(newPassword);
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
            return (
                false,
                "Невозможно удалить пользователя: на него есть ссылки в системе.");
        }
    }

    private static bool UsesWarehouse(UserRole role) =>
        role is UserRole.WarehouseManager
            or UserRole.Storekeeper
            or UserRole.InventorySpecialist;

    private static int? NormalizeWarehouse(UserRole role, int? warehouseId) =>
        UsesWarehouse(role) ? warehouseId : null;

    private static string? ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) ||
            password.Length < MinPasswordLength)
        {
            return $"Пароль должен содержать не менее {MinPasswordLength} символов.";
        }

        return null;
    }

    private static string? Validate(
        string login,
        string fullName,
        UserRole role,
        int? warehouseId)
    {
        if (string.IsNullOrWhiteSpace(login))
        {
            return "Логин обязателен для заполнения.";
        }

        if (string.IsNullOrWhiteSpace(fullName))
        {
            return "ФИО обязательно для заполнения.";
        }

        if (role == UserRole.WarehouseManager && warehouseId == null)
        {
            return "Для заведующего складом необходимо выбрать склад.";
        }

        return null;
    }
}