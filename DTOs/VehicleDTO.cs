namespace ShuttlOps.DTOs
{
    public class VehicleDTO
    {
        public int Id { get; set; }
        public string PlateNumber { get; set; } = null!;
        public string VehicleModel { get; set; } = null!;
        public int Capacity { get; set; }
        public string Status { get; set; } = null!;
    }
}
