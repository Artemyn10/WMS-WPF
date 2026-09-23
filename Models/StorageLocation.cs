using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Models;

public class StorageLocation
{
    public int Id { get; set; }
    public int WarehouseId { get; set; }
    public required string Code { get; set; } // Например: A-01-01
    public int Capacity { get; set; }

    public Warehouse? Warehouse { get; set; }
    public ICollection<Inventory> InventoryRecords { get; set; } = new List<Inventory>();
}
