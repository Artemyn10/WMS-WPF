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
}