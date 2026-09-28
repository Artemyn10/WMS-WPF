namespace WMS.Services;

// Вариант ячейки для выпадающего списка: код + сколько занято
public class LocationOption
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public int Used { get; set; }

    public string Display => $"{Code} (занято {Used} из {Capacity})";
}