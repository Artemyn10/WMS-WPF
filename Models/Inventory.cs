using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Models;

public class Inventory
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int StorageLocationId { get; set; }
    public int Quantity { get; set; }

    public Product? Product { get; set; }
    public StorageLocation? StorageLocation { get; set; }
}
