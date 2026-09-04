using System.ComponentModel.DataAnnotations;
using ShuttlOps.Models;

namespace ShuttlOps.DTOs
{
    public class CreateTripTicketDTO
    {
        public DateTime RequestDate { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Date of trip is required.")]
        public DateOnly TripDate { get; set; }

        [Required(ErrorMessage = "Departure time is required.")]
        public TimeOnly DepartureTime { get; set; }

        [Required(ErrorMessage = "Arrival time is required.")]
        public TimeOnly ArrivalTime { get; set; }

        [Required(ErrorMessage = "Pickup location is required.")]
        [MaxLength(150, ErrorMessage = "Pickup location cannot exceed 150 characters.")]
        public string PickupLocation { get; set; } = null!;

        [Required(ErrorMessage = "Destination / Drop-off location is required.")]
        [MaxLength(150, ErrorMessage = "Drop-off location cannot exceed 150 characters.")]
        public string DropLocation { get; set; } = null!;

        [Required(ErrorMessage = "Purpose is required.")]
        [MaxLength(255, ErrorMessage = "Purpose cannot exceed 255 characters.")]
        public string Purpose { get; set; } = null!;

        public string? Remarks { get; set; }

        public List<PassengerDTO> Passengers { get; set; } = new();
    }

    public class PassengerDTO
    {
        [Required(ErrorMessage = "Passenger name is required.")]
        [MaxLength(100, ErrorMessage = "Passenger name cannot exceed 100 characters.")]
        public string PassengerName { get; set; } = null!;
    }

    public class TripTicketResponseDTO
    {
        public int TicketId { get; set; }
        public string TicketNumber { get; set; } = null!;
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
        public string DriverName { get; set; } = null!;
        public string TripStatus { get; set; } = "Not Started";
        public List<PassengerDTO> Passengers { get; set; } = new();
    }
}

