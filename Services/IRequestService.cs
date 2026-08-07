using ShuttlOps.DTOs;

namespace ShuttlOps.Services
{
    public interface IRequestService
    {
        Task<(bool IsSuccess, string Message)> CreateRequest(CreateTripTicketDTO request);
        Task<List<TripTicketResponseDTO>> GetAllRequests();
    }
}
