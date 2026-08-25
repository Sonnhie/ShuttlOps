namespace ShuttlOps.DTOs
{
    public class CreateTripTicketDTO
    {
        public DateTime RequestDate { get; set; } = DateTime.Now;
        public string RequestedBy { get; set; } = null!;

        public DateOnly TripDate { get; set; }
        public TimeOnly DepartureTime { get; set; }
        public TimeOnly ArrivalTime { get; set; }

        // Required Locations & Details
        public string PickupLocation { get; set; } = null!;
        public string DropLocation { get; set; } = null!;
        public string Purpose { get; set; } = null!;
        public string? Remarks { get; set; }

        // Associated Passengers
        public List<PassengerDTO> Passengers { get; set; } = new();
    }

    public class PassengerDTO
    {
        public string PassengerName { get; set; } = null!;
    }

    public class TripTicketResponseDTO
    {
        public int TicketId { get; set; }
        public string TicketNumber { get; set; } = null!; // Included AFTER generation in backend
        public DateOnly RequestDate { get; set; }
        public DateOnly TripDate { get; set; }
        public TimeOnly DepartureTime { get; set; }
        public TimeOnly ArrivalTime { get; set; }
        public string PickupLocation { get; set; } = null!;
        public string DropLocation { get; set; } = null!;
        public string Purpose { get; set; } = null!;
        public string? Remarks { get; set; }
        public string ApprovalStatus { get; set; } = "Pending";
        public string RequestDepartment { get; set; } = null!;
        public string Requestor { get; set; } = null!;

        public List<PassengerDTO> Passengers { get; set; } = new();
    }
}
