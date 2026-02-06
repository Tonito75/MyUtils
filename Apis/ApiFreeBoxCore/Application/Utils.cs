namespace Application
{
    public static class Utils
    {
        public static List<LanDevice> ParseFreeBoxDeviceToMachines(List<FreeBoxDevice> devices)
        {
            var result = new List<LanDevice>();

            foreach (var device in devices)
            {
                var (ip, type) = GetIp(device);
                result.Add(new LanDevice
                {
                    IpAddress = ip,
                    AddressType = type,
                    OriginalName = GetHost(device),
                    MacAddress = GetMac(device),
                    Isfavourite = false,
                    IsConnected = IsConnected(device),
                    Vendor = GetVendor(device),
                    LastConnected = GetLastConnected(device),
                    ConnectedSince = GetConnectedSince(device),
                    HostType = GetHostType(device),
                });
            }
            return result;
        }

        private static string GetHostType(FreeBoxDevice device)
        {
            return device.HostType;
        }

        private static DateTime GetConnectedSince(FreeBoxDevice device)
        {
            return DateTimeOffset.FromUnixTimeSeconds(device.FirstActivity).LocalDateTime;
        }

        private static DateTime GetLastConnected(FreeBoxDevice device)
        {
            return DateTimeOffset.FromUnixTimeSeconds(device.LastTimeReachable).LocalDateTime;
        }

        private static string GetVendor(FreeBoxDevice device)
        {
            return device.VendorName;
        }

        private static string GetMac(FreeBoxDevice device)
        {
            if (device.L2Ident == null)
            {
                return string.Empty;
            }

            return device.L2Ident.Id;
        }

        private static string GetHost(FreeBoxDevice device)
        {
            if (!string.IsNullOrEmpty(device.PrimaryName))
            {
                return device.PrimaryName;
            }

            return device.DefaultName;
        }

        private static bool IsConnected(FreeBoxDevice device)
        {
            return device.Reachable;
        }

        private static (string, AddressType) GetIp(FreeBoxDevice device)
        {
            if (device.L3connectivities == null)
            {
                return (string.Empty, AddressType.Unknown);
            }
            var IPV4l3Connectivity = device.L3connectivities.Where(c => c.Af == "ipv4").ToList().LastOrDefault();

            if (IPV4l3Connectivity == null)
            {
                var IPV6l3Connectivity = device.L3connectivities.Where(c => c.Af == "ipv6").ToList().LastOrDefault();
                if (IPV6l3Connectivity != null)
                {
                    return (IPV6l3Connectivity.Addr, AddressType.IPV6);
                }
                return (string.Empty, AddressType.Unknown);
            }

            return (IPV4l3Connectivity.Addr, AddressType.IPV4);
        }
    }
}
