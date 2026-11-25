using Microsoft.EntityFrameworkCore;
using WMS.Data;
using WMS.Models;

namespace WMS.Services;

public class WarehouseEmployeeService
{
    private readonly IDbContextFactory<WmsDbContext> _contextFactory;

    public WarehouseEmployeeService(
        IDbContextFactory<WmsDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    /// <summary>
    /// Возвращает обычных сотрудников и пользователей, прикреплённых к складу.
    /// warehouseId = 0 означает все склады.
    /// </summary>
    public async Task<List<WarehouseStaffOption>> GetStaffAsync(
        string? search = null,
        int warehouseId = 0)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var employeesQuery = context.WarehouseEmployees
            .AsNoTracking()
            .Include(employee => employee.Warehouse)
            .AsQueryable();

        var usersQuery = context.Users
            .AsNoTracking()
            .Include(user => user.Warehouse)
            .Where(user => user.WarehouseId != null)
            .AsQueryable();

        if (warehouseId > 0)
        {
            employeesQuery = employeesQuery.Where(
                employee => employee.WarehouseId == warehouseId);
            usersQuery = usersQuery.Where(
                user => user.WarehouseId == warehouseId);
        }

        if (CurrentSession.WarehouseScopeId is int scopeId)
        {
            employeesQuery = employeesQuery.Where(
                employee => employee.WarehouseId == scopeId);
            usersQuery = usersQuery.Where(
                user => user.WarehouseId == scopeId);
        }

        var employees = await employeesQuery.ToListAsync();
        var users = await usersQuery.ToListAsync();

        var result = employees.Select(employee => new WarehouseStaffOption
        {
            Id = employee.Id,
            WarehouseId = employee.WarehouseId,
            WarehouseName = employee.Warehouse?.Name ?? "—",
            Type = WarehouseStaffType.Employee,
            FullName = employee.FullName,
            Position = employee.Position,
            Phone = employee.Phone,
            Email = employee.Email,
            IsResponsible = employee.IsResponsible
        }).ToList();

        result.AddRange(users.Select(user => new WarehouseStaffOption
        {
            Id = user.Id,
            WarehouseId = user.WarehouseId!.Value,
            WarehouseName = user.Warehouse?.Name ?? "—",
            Type = WarehouseStaffType.User,
            FullName = user.FullName,
            Login = user.Login,
            Role = user.RoleDisplay,
            IsResponsible = user.IsWarehouseResponsible
        }));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var value = search.Trim();
            result = result
                .Where(item =>
                    item.FullName.Contains(
                        value,
                        StringComparison.CurrentCultureIgnoreCase) ||
                    (item.Position?.Contains(
                        value,
                        StringComparison.CurrentCultureIgnoreCase) ?? false) ||
                    (item.Login?.Contains(
                        value,
                        StringComparison.CurrentCultureIgnoreCase) ?? false) ||
                    (item.Role?.Contains(
                        value,
                        StringComparison.CurrentCultureIgnoreCase) ?? false))
                .ToList();
        }

        return result
            .OrderBy(item => item.WarehouseName)
            .ThenByDescending(item => item.IsResponsible)
            .ThenBy(item => item.FullName)
            .ToList();
    }

    public async Task<(bool Success, string? Error)> SetResponsibilityAsync(
        int warehouseId,
        int personId,
        WarehouseStaffType type,
        bool isResponsible)
    {
        if (warehouseId <= 0 || personId <= 0)
        {
            return (false, "Не удалось определить склад или сотрудника.");
        }

        await using var context = await _contextFactory.CreateDbContextAsync();

        if (type == WarehouseStaffType.Employee)
        {
            var employee = await context.WarehouseEmployees
                .FirstOrDefaultAsync(item =>
                    item.Id == personId &&
                    item.WarehouseId == warehouseId);

            if (employee == null)
            {
                return (false, "Сотрудник не найден на выбранном складе.");
            }

            employee.IsResponsible = isResponsible;
        }
        else
        {
            var user = await context.Users
                .FirstOrDefaultAsync(item =>
                    item.Id == personId &&
                    item.WarehouseId == warehouseId);

            if (user == null)
            {
                return (false, "Пользователь не прикреплён к выбранному складу.");
            }

            user.IsWarehouseResponsible = isResponsible;
        }

        await context.SaveChangesAsync();
        return (true, null);
    }

    /// <summary>warehouseId = 0 означает сотрудников всех складов.</summary>
    public async Task<List<WarehouseEmployee>> GetAllAsync(
        string? search = null,
        int warehouseId = 0)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var query = context.WarehouseEmployees
            .Include(employee => employee.Warehouse)
            .AsQueryable();

        if (warehouseId > 0)
        {
            query = query.Where(employee =>
                employee.WarehouseId == warehouseId);
        }

        if (CurrentSession.WarehouseScopeId is int scopeId)
        {
            query = query.Where(employee =>
                employee.WarehouseId == scopeId);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var value = search.Trim().ToLower();
            query = query.Where(employee =>
                employee.FullName.ToLower().Contains(value) ||
                employee.Position.ToLower().Contains(value));
        }

        return await query
            .OrderBy(employee => employee.Warehouse!.Name)
            .ThenByDescending(employee => employee.IsResponsible)
            .ThenBy(employee => employee.FullName)
            .ToListAsync();
    }

    public async Task<(bool Success, string? Error)> AddAsync(
        WarehouseEmployee employee)
    {
        var validationError = Validate(employee);
        if (validationError != null)
        {
            return (false, validationError);
        }

        await using var context = await _contextFactory.CreateDbContextAsync();

        context.WarehouseEmployees.Add(new WarehouseEmployee
        {
            WarehouseId = employee.WarehouseId,
            FullName = employee.FullName.Trim(),
            Position = employee.Position.Trim(),
            Phone = Clean(employee.Phone),
            Email = Clean(employee.Email),
            IsResponsible = employee.IsResponsible
        });

        await context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(
        WarehouseEmployee employee)
    {
        var validationError = Validate(employee);
        if (validationError != null)
        {
            return (false, validationError);
        }

        await using var context = await _contextFactory.CreateDbContextAsync();

        var existing = await context.WarehouseEmployees.FindAsync(employee.Id);
        if (existing == null)
        {
            return (false, "Сотрудник не найден.");
        }

        existing.WarehouseId = employee.WarehouseId;
        existing.FullName = employee.FullName.Trim();
        existing.Position = employee.Position.Trim();
        existing.Phone = Clean(employee.Phone);
        existing.Email = Clean(employee.Email);
        existing.IsResponsible = employee.IsResponsible;

        await context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var employee = await context.WarehouseEmployees.FindAsync(id);
        if (employee == null)
        {
            return (false, "Сотрудник не найден.");
        }

        context.WarehouseEmployees.Remove(employee);
        await context.SaveChangesAsync();
        return (true, null);
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? Validate(WarehouseEmployee employee)
    {
        if (employee.WarehouseId <= 0)
        {
            return "Необходимо выбрать склад.";
        }

        if (string.IsNullOrWhiteSpace(employee.FullName))
        {
            return "ФИО сотрудника обязательно для заполнения.";
        }

        if (string.IsNullOrWhiteSpace(employee.Position))
        {
            return "Должность обязательна для заполнения.";
        }

        if (!string.IsNullOrWhiteSpace(employee.Email) &&
            !employee.Email.Contains('@'))
        {
            return "Email указан некорректно.";
        }

        return null;
    }
}