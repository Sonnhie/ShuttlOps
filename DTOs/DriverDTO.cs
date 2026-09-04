namespace ShuttlOps.DTOs
{
    public class DriverDTO
    {
        public int Id { get; set; }
        public string DriverName { get; set; } = null!;
        public string LicenseNumber { get; set; } = null!;
        public string Status { get; set; } = null!;
        public string ContactNumber { get; set; } = null!;
    }
}
