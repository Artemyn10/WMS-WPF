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

    public static List<UserRoleOption> All => new()
    {
        new UserRoleOption(UserRole.Administrator, "Администратор"),
        new UserRoleOption(UserRole.Storekeeper, "Кладовщик")
    };
}