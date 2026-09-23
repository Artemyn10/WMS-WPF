using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Models;

public class Receipt
{
    public int Id { get; set; }
    public int SupplierId { get; set; }
    public int WarehouseId { get; set; }
    public required string DocumentNumber { get; set; }
    public DateTime Date { get; set; }
    public ReceiptStatus Status { get; set; }

    public Supplier? Supplier { get; set; }
    public Warehouse? Warehouse { get; set; }
    public ICollection<ReceiptItem> Items { get; set; } = new List<ReceiptItem>();
}
