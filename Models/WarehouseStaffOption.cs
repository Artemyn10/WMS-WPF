namespace WMS.Models;

public enum WarehouseStaffType
{
    Employee,
    User
}

public class WarehouseStaffOption
{
    public int Id { get; set; }
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public WarehouseStaffType Type { get; set; }

    public required string FullName { get; set; }
    public string? Position { get; set; }
    public string? Login { get; set; }
    public string? Role { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }

    public bool IsResponsible { get; set; }

    public string TypeDisplay =>
        Type == WarehouseStaffType.User ? "Пользователь системы" : "Сотрудник";

    public string DetailsDisplay =>
        Type == WarehouseStaffType.User
            ? $"{Role} · {Login}"
            : Position ?? "—";

    public string DisplayName =>
        Type == WarehouseStaffType.User
            ? $"{FullName} (пользователь)"
            : $"{FullName} (сотрудник)";
}