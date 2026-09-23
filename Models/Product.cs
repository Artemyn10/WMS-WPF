using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Models;

public class Product
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Article { get; set; }
    public string? Barcode { get; set; }
    public int CategoryId { get; set; }
    public required string Unit { get; set; }
    public string? Description { get; set; }

    public Category? Category { get; set; }
    public ICollection<Inventory> InventoryRecords { get; set; } = new List<Inventory>();
}
