using WMS.Models;

namespace WMS.Services;

public static class CurrentSession
{
    public static User? CurrentUser { get; private set; }

    public static void SignIn(User user)
    {
        CurrentUser = user;
    }

    public static void SignOut()
    {
        CurrentUser = null;
    }

    public static bool IsAdministrator => CurrentUser?.Role == UserRole.Administrator;

    /// Доступен ли текущему пользователю раздел
    public static bool CanAccess(AppSection section) =>
        CurrentUser != null && RolePermissions.IsAllowed(CurrentUser.Role, section);

    /// Склад, которым ограничен пользователь.
    /// null - ограничения нет, пользователь видит все склады.
    /// 0 - заведующий без склада: не совпадает ни с одним складом, поэтому он ничего не видит.
    public static int? WarehouseScopeId
    {
        get
        {
            var user = CurrentUser;
            if (user == null)
            {
                return null;
            }

            return user.Role switch
            {
                UserRole.WarehouseManager => user.WarehouseId ?? 0,
                UserRole.Storekeeper or UserRole.InventorySpecialist => user.WarehouseId,
                _ => null
            };
        }
    }

    public static bool CanAccessWarehouse(int warehouseId) =>
        WarehouseScopeId is not int scopeId || scopeId == warehouseId;
}