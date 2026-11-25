using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace WMS.Models;

public class Warehouse
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Address { get; set; }

    public decimal? Area { get; set; }
    public string? Purpose { get; set; }
    public string? WorkingHours { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }

    public ICollection<StorageLocation> StorageLocations { get; set; } =
        new List<StorageLocation>();

    public ICollection<WarehouseEmployee> Employees { get; set; } =
        new List<WarehouseEmployee>();

    [InverseProperty(nameof(WarehouseTransfer.SourceWarehouse))]
    public ICollection<WarehouseTransfer> OutgoingTransfers { get; set; } =
        new List<WarehouseTransfer>();

    [InverseProperty(nameof(WarehouseTransfer.DestinationWarehouse))]
    public ICollection<WarehouseTransfer> IncomingTransfers { get; set; } =
        new List<WarehouseTransfer>();

    [NotMapped]
    public string ResponsibleNames =>
        string.Join(", ", Employees.Where(employee => employee.IsResponsible)
            .Select(employee => employee.FullName));
}