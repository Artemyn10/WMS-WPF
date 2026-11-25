using WMS.Helpers;
using WMS.Models;

namespace WMS.Services;

public class UserRoleOption
{
    public UserRole Value { get; }
    public string Display { get; }

    public UserRoleOption(UserRole value, string display)
    {
        Value = value;
        Display = display;
    }

    // Порядок как в техническом задании
    public static List<UserRoleOption> All => new[]
    {
        UserRole.Administrator,
        UserRole.Director,
        UserRole.WarehouseManager,
        UserRole.Storekeeper,
        UserRole.PurchasingManager,
        UserRole.SalesManager,
        UserRole.InventorySpecialist
    }
    .Select(role => new UserRoleOption(role, UserRoleNames.ToRussian(role)))
    .ToList();
}