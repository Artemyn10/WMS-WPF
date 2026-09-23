using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Models;

public class Shipment
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public DateTime Date { get; set; }
    public ShipmentStatus Status { get; set; }

    public Order? Order { get; set; }
    public ICollection<ShipmentItem> Items { get; set; } = new List<ShipmentItem>();
}
