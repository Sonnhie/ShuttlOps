using Azure.Core;
using Microsoft.AspNetCore.Mvc;
using ShuttlOps.DTOs;

namespace ShuttlOps.Services.Interfaces
{
    public interface IGAService
    {
        Task<List<VehicleDTO>> GetAllVehicle();
        Task<List<DriverDTO>> GetAllDriver();
        Task<List<TripTicketResponseDTO>> GetApproveRequest();
        Task<(bool isSuccesss, string message)> UpdateDriverDetails(DriverDTO Driver);
        Task<(bool isSuccess, string message)> DeleteDriverDetails(int id);
        Task<(bool isSuccess, string message)> CreateDriverDetails(DriverDTO Driver);
        Task<(bool isSuccesss, string message)> CreateVehicleDetails(VehicleDTO Vehicle);
        Task<(bool isSuccess, string message)> UpdateVehicleDetails(VehicleDTO Vehicle);
        Task<(bool isSuccess, string message)> DeleteVehicleDetails(int id);
        Task<(bool isSuccess, string message)> AssignDriver(DispatchDto Dispatch);
        Task<UserDto> GetUserInfo();
    }
}
