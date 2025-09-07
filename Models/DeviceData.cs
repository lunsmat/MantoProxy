using System.Text.Json;

namespace MantoProxy.Models
{
    class DeviceData
    {
        public int Id { get; set; }

        public required string Name { get; set; }

        public required string MacAddress { get; set; }

        public required bool AllowConnection { get; set; }

        public required string? Filters { get; set; }

        public string[] FiltersList => Filters != null ? Filters.Split('\n') : [];
    }
}