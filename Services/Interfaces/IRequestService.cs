using ShuttlOps.DTOs;

namespace ShuttlOps.Services.Interfaces
{
    public interface IRequestService
    {
        Task<(bool IsSuccess, string Message)> CreateRequest(CreateTripTicketDTO request);
        Task<List<TripTicketResponseDTO>> GetAllRequests();
        Task<(bool isSuccess, string message)> ProcessApproval(string status, int id);
        Task<(bool isSuccess, string message)> DeleteRequestApproval(int id);
        Task<List<DriverDTO>> GetDriver();
        Task<List<VehicleDTO>> GetVehicle();
        Task<string> GetPlatenumber(int id);
        Task<int> GetCapacity(int id);
        Task<UserDto> GetUserInfo();
        Task<string> GetVehicleStatus(int id);
        Task<List<TripTicketResponseDTO>> GetScheduledTrip();
        Task<(bool isSuccess, string message)> CompleteTrip(int ticketId);
        Task<List<TripTicketResponseDTO>> GetOnTripScheduled();
        Task<(bool isSuccess, string message)> SecurityLogs(SecurityLogsDTO securityLogsDTO);
        Task<(bool isSuccess, string message)> DispatchConfirm(int ticketId);
    }
}
