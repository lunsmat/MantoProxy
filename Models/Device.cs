namespace MantoProxy.Models
{
    class Device
    {
        public required int Id { get; set; }

        public required string Name { get; set; }

        public required string MacAddress { get; set; }

        public required bool AllowConnection { get; set; }
    }
}