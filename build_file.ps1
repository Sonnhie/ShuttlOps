$content = @"
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShuttlOps.DTOs;
using ShuttlOps.Models;
using ShuttlOps.Services.Interfaces;
using System.Security.Claims;

namespace ShuttlOps.Services.MainServices
{
    [Authorize]
    public class RequestService(
        ShuttlOpsDbContext dbContext,
        ILogger<RequestService> logger,
        IHttpContextAccessor httpContextAccessor
    ) : IRequestService
    {
        public async Task<(bool IsSuccess, string Message)> CreateRequest([FromBody] CreateTripTicketDTO ticketDTO)
        {
            await ResolveTransitStatuses();
            try
            {
                if (ticketDTO == null)
                {
                    return (false, "Invalid request payload.");
                }
