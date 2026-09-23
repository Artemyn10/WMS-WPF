using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Models;

public class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public int WarehouseId { get; set; }
    public required string OrderNumber { get; set; }
    public DateTime Date { get; set; }
    public OrderStatus Status { get; set; }

    public Customer? Customer { get; set; }
    public Warehouse? Warehouse { get; set; }
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
