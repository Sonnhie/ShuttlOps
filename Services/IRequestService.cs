using ShuttlOps.DTOs;

namespace ShuttlOps.Services
{
    public interface IRequestService
    {
        Task<(bool IsSuccess, string Message)> CreateRequest(CreateTripTicketDTO request);
        Task<List<TripTicketResponseDTO>> GetAllRequests();
        Task<(bool isSuccess, string message)> SectionRequest(string status, int id);
        Task<(bool isSuccess, string message)> DeleteRequestApproval(int id);
    }
}
