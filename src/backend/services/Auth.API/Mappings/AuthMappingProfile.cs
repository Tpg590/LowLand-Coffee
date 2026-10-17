using AutoMapper;
using Auth.API.DTOs;
using Auth.API.Entities;

namespace Auth.API.Mappings;

public class AuthMappingProfile : Profile
{
    public AuthMappingProfile()
    {
        // -------------------------------------------------------------
        // 1. Entity to DTO Mappings
        // -------------------------------------------------------------
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role != null ? src.Role.Name : string.Empty));

        CreateMap<User, UserProfileDto>()
            .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role != null ? src.Role.Name : string.Empty));

        CreateMap<Role, RoleDto>();

        // -------------------------------------------------------------
        // 2. Request to Entity Mappings
        // -------------------------------------------------------------
        CreateMap<RegisterRequest, User>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.RoleId, opt => opt.Ignore())
            .ForMember(dest => dest.Role, opt => opt.Ignore())
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName.Trim()))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email.Trim().ToLower()))
            .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => !string.IsNullOrWhiteSpace(src.PhoneNumber) ? src.PhoneNumber.Trim() : null))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(_ => true))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow));

        CreateMap<CreateStaffRequest, User>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.RoleId, opt => opt.Ignore())
            .ForMember(dest => dest.Role, opt => opt.Ignore())
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName.Trim()))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email.Trim().ToLower()))
            .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => !string.IsNullOrWhiteSpace(src.PhoneNumber) ? src.PhoneNumber.Trim() : null))
            .ForMember(dest => dest.Address, opt => opt.MapFrom(src => !string.IsNullOrWhiteSpace(src.Address) ? src.Address.Trim() : null))
            .ForMember(dest => dest.AvatarUrl, opt => opt.MapFrom(src => !string.IsNullOrWhiteSpace(src.AvatarUrl) ? src.AvatarUrl.Trim() : null))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(_ => true))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow));

        CreateMap<UpdateUserRequest, User>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.RoleId, opt => opt.Ignore())
            .ForMember(dest => dest.Role, opt => opt.Ignore())
            .ForMember(dest => dest.Email, opt => opt.Ignore())
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName.Trim()))
            .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom((src, dest) => !string.IsNullOrWhiteSpace(src.PhoneNumber) ? src.PhoneNumber.Trim() : dest.PhoneNumber))
            .ForMember(dest => dest.Address, opt => opt.MapFrom((src, dest) => src.Address != null ? src.Address.Trim() : dest.Address))
            .ForMember(dest => dest.AvatarUrl, opt => opt.MapFrom((src, dest) => !string.IsNullOrWhiteSpace(src.AvatarUrl) ? src.AvatarUrl.Trim() : dest.AvatarUrl))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow));

        CreateMap<UpdateProfileRequest, User>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.RoleId, opt => opt.Ignore())
            .ForMember(dest => dest.Role, opt => opt.Ignore())
            .ForMember(dest => dest.Email, opt => opt.Ignore())
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName.Trim()))
            .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom((src, dest) => !string.IsNullOrWhiteSpace(src.PhoneNumber) ? src.PhoneNumber.Trim() : dest.PhoneNumber))
            .ForMember(dest => dest.Address, opt => opt.MapFrom((src, dest) => src.Address != null ? src.Address.Trim() : dest.Address))
            .ForMember(dest => dest.AvatarUrl, opt => opt.MapFrom((src, dest) => !string.IsNullOrWhiteSpace(src.AvatarUrl) ? src.AvatarUrl.Trim() : dest.AvatarUrl))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow));
    }
}
