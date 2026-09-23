using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Models;

public class StockMovement
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int? FromLocationId { get; set; }
    public int? ToLocationId { get; set; }
    public int Quantity { get; set; }
    public OperationType OperationType { get; set; }
    public DateTime Date { get; set; }

    public Product? Product { get; set; }
    public StorageLocation? FromLocation { get; set; }
    public StorageLocation? ToLocation { get; set; }
}
