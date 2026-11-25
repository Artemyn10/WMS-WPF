using WMS.Models;

namespace WMS.Helpers;

public static class UserRoleNames
{
    public static string ToRussian(UserRole role) => role switch
    {
        UserRole.Administrator => "Администратор системы",
        UserRole.Director => "Руководитель организации",
        UserRole.WarehouseManager => "Заведующий складом",
        UserRole.Storekeeper => "Кладовщик",
        UserRole.PurchasingManager => "Менеджер по закупкам",
        UserRole.SalesManager => "Менеджер по продажам",
        UserRole.InventorySpecialist => "Специалист по инвентаризации",
        _ => role.ToString()
    };
}