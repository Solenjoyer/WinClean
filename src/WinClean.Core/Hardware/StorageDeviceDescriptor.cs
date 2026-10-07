namespace WinClean.Core.Hardware;

public readonly record struct StorageDeviceDescriptor(bool RemovableMedia, string Vendor, string Product, string Revision, string SerialNumber, int BusType)
{
    public string BusName => StorageBusType.Name(BusType);

    /// <summary>"Samsung SSD 990 PRO 2TB": vendor and product, without the vendor repeated.</summary>
    public string Model
    {
        get
        {
            if (Vendor.Length == 0)
            {
                return Product;
            }

            return Product.StartsWith(Vendor, StringComparison.OrdinalIgnoreCase) ? Product : (Vendor + " " + Product).Trim();
        }
    }
}
