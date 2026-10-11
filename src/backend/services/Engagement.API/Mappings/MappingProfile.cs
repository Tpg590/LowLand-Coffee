using AutoMapper;
using Engagement.API.DTOs.Loyalty;
using Engagement.API.DTOs.Vouchers;
using Engagement.API.Entities;

namespace Engagement.API.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // -------------------------------------------------------------
        // 1. Voucher Mappings
        // -------------------------------------------------------------
        CreateMap<PromoVouchers, VoucherDto>();

        CreateMap<CreateVoucherRequest, PromoVouchers>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(_ => Guid.NewGuid()))
            .ForMember(dest => dest.Code, opt => opt.MapFrom(src => src.Code.Trim().ToUpper()))
            .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title.Trim()))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description != null ? src.Description.Trim() : null))
            .ForMember(dest => dest.DiscountType, opt => opt.MapFrom(src => src.DiscountType.ToUpper()))
            .ForMember(dest => dest.UsedQuantity, opt => opt.MapFrom(_ => 0))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow));

        // -------------------------------------------------------------
        // 2. Loyalty Mappings
        // -------------------------------------------------------------
        CreateMap<LoyaltyTiers, LoyaltyTierDto>();

        CreateMap<LoyaltyPointTransactions, PointTransactionDto>();

        CreateMap<LoyaltyMembers, LoyaltyMemberProfileDto>()
            .ForMember(dest => dest.TierCode, opt => opt.MapFrom(src => src.Tier != null ? src.Tier.TierCode : "BRONZE"))
            .ForMember(dest => dest.TierName, opt => opt.MapFrom(src => src.Tier != null ? src.Tier.TierName : "Đồng"))
            .ForMember(dest => dest.PointMultiplier, opt => opt.MapFrom(src => src.Tier != null ? src.Tier.PointMultiplier : 1.0m))
            .ForMember(dest => dest.BirthdayDiscountPercent, opt => opt.MapFrom(src => src.Tier != null ? src.Tier.BirthdayDiscountPercent : 0m))
            .ForMember(dest => dest.FreeToppingCount, opt => opt.MapFrom(src => src.Tier != null ? src.Tier.FreeToppingCount : 0))
            .ForMember(dest => dest.FreeToppingMinOrder, opt => opt.MapFrom(src => src.Tier != null ? src.Tier.FreeToppingMinOrder : 0m))
            .ForMember(dest => dest.BenefitsDescription, opt => opt.MapFrom(src => src.Tier != null ? src.Tier.BenefitsDescription : null));
    }
}
