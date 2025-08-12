namespace MantoProxy.Models
{
    class DeviceLog
    {
        public int Id { get; set; }

        public required int DeviceId { get; set; }

        public required string HttpMethod { get; set; }

        public required string HttpUrl { get; set; }

        public required string HttpHeaders { get; set; }

        public required string HttpBody { get; set; }

        public required DateTime CreatedAt { get; set; }

        public required DateTime UpdatedAt { get; set; }
    }
}