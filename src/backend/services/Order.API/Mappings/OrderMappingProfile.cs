using AutoMapper;
using Order.API.DTOs;
using Order.API.Entities;

namespace Order.API.Mappings;

public class OrderMappingProfile : Profile
{
    public OrderMappingProfile()
    {
        // 1. OrderItem Mapping
        CreateMap<OrderItem, OrderItemDto>();

        // 2. OrderPayment Mapping
        CreateMap<OrderPayment, OrderPaymentDto>();

        // 3. Order -> OrderDto Mapping
        CreateMap<Entities.Order, OrderDto>()
            .ForMember(dest => dest.TotalItems, opt => opt.MapFrom(src => src.OrderItems.Sum(i => i.Quantity)))
            .ForMember(dest => dest.ItemsSummary, opt => opt.MapFrom(src =>
                string.Join(", ", src.OrderItems.Select(i => $"{i.Quantity}x {i.ProductName}"))))
            .ForMember(dest => dest.PaymentMethod, opt => opt.MapFrom(src =>
                src.OrderPayments.OrderByDescending(p => p.PaymentStatus == PaymentStatus.SUCCESS ? 1 : 0)
                                 .ThenByDescending(p => p.CreatedAt)
                                 .Select(p => p.PaymentMethod).FirstOrDefault()))
            .ForMember(dest => dest.PaymentStatus, opt => opt.MapFrom(src =>
                src.OrderPayments.Any(p => p.PaymentStatus == PaymentStatus.SUCCESS)
                    ? PaymentStatus.SUCCESS
                    : src.OrderPayments.OrderByDescending(p => p.CreatedAt).Select(p => p.PaymentStatus).FirstOrDefault()));

        // 4. Order -> OrderDetailDto Mapping
        CreateMap<Entities.Order, OrderDetailDto>()
            .ForMember(dest => dest.TotalItems, opt => opt.MapFrom(src => src.OrderItems.Sum(i => i.Quantity)))
            .ForMember(dest => dest.PaymentMethod, opt => opt.MapFrom(src =>
                src.OrderPayments.OrderByDescending(p => p.PaymentStatus == PaymentStatus.SUCCESS ? 1 : 0)
                                 .ThenByDescending(p => p.CreatedAt)
                                 .Select(p => p.PaymentMethod).FirstOrDefault()))
            .ForMember(dest => dest.PaymentStatus, opt => opt.MapFrom(src =>
                src.OrderPayments.Any(p => p.PaymentStatus == PaymentStatus.SUCCESS)
                    ? PaymentStatus.SUCCESS
                    : src.OrderPayments.OrderByDescending(p => p.CreatedAt).Select(p => p.PaymentStatus).FirstOrDefault()))
            .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.OrderItems))
            .ForMember(dest => dest.Payments, opt => opt.MapFrom(src => src.OrderPayments))
            .ForMember(dest => dest.VnPayPayment, opt => opt.Ignore());
    }
}
