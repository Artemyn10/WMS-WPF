using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Models;

public class ShipmentItem
{
    public int Id { get; set; }
    public int ShipmentId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }

    public Shipment? Shipment { get; set; }
    public Product? Product { get; set; }
}
