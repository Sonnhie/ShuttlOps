namespace ShuttlOps.DTOs
{
    public class DispatchDto
    {
        public int TicketId { get; set; }
        public string VehicleStatus { get; set; } = null!;
        public string Remarks { get; set; } = null!;
        public string DriverName { get; set; } = null!;
        public string PlateNumber { get; set; } = null!;
        public string PicId { get; set; } = null!;
        public string PicName { get; set; }  = null!;
        public DateTime ConfirmedAt { get; set; }
        public int VehicleId { get; set; }
        public int DriverId { get; set; }
        public string Status { get; set; } = null!;
    }
}
