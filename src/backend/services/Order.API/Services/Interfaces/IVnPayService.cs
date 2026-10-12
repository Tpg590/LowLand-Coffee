using Microsoft.AspNetCore.Http;
using Order.API.DTOs;
using Order.API.Entities;

namespace Order.API.Services.Interfaces;

public interface IVnPayService
{
    VnPayPaymentResponseDto CreatePaymentUrl(Entities.Order order, string clientIp, string? customReturnUrl = null);
    Task<VnPayIpnResponseDto> ProcessIpnCallbackAsync(IQueryCollection queryParams, CancellationToken cancellationToken = default);
    Task<VnPayReturnResultDto> ProcessPaymentReturnAsync(IQueryCollection queryParams, CancellationToken cancellationToken = default);
}
