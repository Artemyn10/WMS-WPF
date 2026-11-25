using System.ComponentModel.DataAnnotations.Schema;
using WMS.Helpers;

namespace WMS.Models;

public class User
{
    public int Id { get; set; }
    public required string Login { get; set; }
    public required string PasswordHash { get; set; }
    public required string FullName { get; set; }
    public UserRole Role { get; set; }

    // Склад, к которому привязан пользователь.
    public int? WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }

    // Пользователь отмечен ответственным за свой склад.
    public bool IsWarehouseResponsible { get; set; }

    [NotMapped]
    public string RoleDisplay => UserRoleNames.ToRussian(Role);
}