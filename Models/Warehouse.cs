using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Models;

public class Warehouse
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Address { get; set; }

    public ICollection<StorageLocation> StorageLocations { get; set; } = new List<StorageLocation>();
}
