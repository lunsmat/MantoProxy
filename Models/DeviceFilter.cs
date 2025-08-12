namespace MantoProxy.Models
{
    class DeviceFilter
    {
        public required int DeviceId { get;  set; }

        public required string MacAddress { get; set; }

        public required string Filters { get; set; }
    }
}