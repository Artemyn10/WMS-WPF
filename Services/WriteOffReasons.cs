namespace WMS.Services;

public static class WriteOffReasons
{
    public static List<string> All => new()
    {
        "Брак",
        "Истёк срок годности",
        "Повреждение при хранении",
        "Утрата / недостача",
        "Прочее"
    };
}