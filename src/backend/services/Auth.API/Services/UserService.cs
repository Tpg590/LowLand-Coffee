using AutoMapper;
using Auth.API.DTOs;
using Auth.API.Entities;
using Auth.API.Repositories.Interfaces;
using Auth.API.Services.Interfaces;

namespace Auth.API.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly ITokenStore _tokenStore;
    private readonly IMapper _mapper;

    public UserService(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        ITokenStore tokenStore,
        IMapper mapper)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _tokenStore = tokenStore;
        _mapper = mapper;
    }

    public async Task<ApiResponse<PagedResult<UserDto>>> GetUsersAsync(UserQueryParameters query, CancellationToken cancellationToken = default)
    {
        var pagedUsers = await _userRepository.GetPagedAsync(
            query.Search,
            query.RoleName,
            query.IsActive,
            query.PageNumber,
            query.PageSize,
            cancellationToken);

        var result = new PagedResult<UserDto>
        {
            Items = _mapper.Map<IEnumerable<UserDto>>(pagedUsers.Items),
            TotalItems = pagedUsers.TotalItems,
            PageNumber = pagedUsers.PageNumber,
            PageSize = pagedUsers.PageSize
        };

        return ApiResponse<PagedResult<UserDto>>.Success(result, "Lấy danh sách người dùng thành công");
    }

    public async Task<ApiResponse<UserDto>> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        if (user == null)
        {
            return ApiResponse<UserDto>.Fail("Không tìm thấy người dùng", 404);
        }

        return ApiResponse<UserDto>.Success(_mapper.Map<UserDto>(user), "Lấy thông tin người dùng thành công");
    }

    public async Task<ApiResponse<UserDto>> CreateStaffAsync(CreateStaffRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLower();
        var emailExists = await _userRepository.ExistsByEmailAsync(normalizedEmail, null, cancellationToken);
        if (emailExists)
        {
            return ApiResponse<UserDto>.Fail("Email đã được sử dụng", 400);
        }

        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            var phoneExists = await _userRepository.ExistsByPhoneAsync(request.PhoneNumber.Trim(), null, cancellationToken);
            if (phoneExists)
            {
                return ApiResponse<UserDto>.Fail("Số điện thoại đã được sử dụng", 400);
            }
        }

        var staffRole = await _roleRepository.GetByNameAsync("Staff", cancellationToken);
        if (staffRole == null)
        {
            staffRole = await _roleRepository.GetByIdAsync(Guid.Parse("22222222-2222-2222-2222-222222222222"), cancellationToken);
        }

        if (staffRole == null)
        {
            return ApiResponse<UserDto>.Fail("Không tìm thấy vai trò Staff trong hệ thống", 500);
        }

        var user = _mapper.Map<User>(request);
        user.Id = Guid.NewGuid();
        user.RoleId = staffRole.Id;
        user.Role = staffRole;
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password.Trim());

        var created = await _userRepository.AddAsync(user, cancellationToken);
        return ApiResponse<UserDto>.Success(_mapper.Map<UserDto>(created), "Tạo tài khoản nhân viên thành công", 201);
    }

    public async Task<ApiResponse<UserDto>> UpdateStatusAsync(Guid id, UpdateUserStatusRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        if (user == null)
        {
            return ApiResponse<UserDto>.Fail("Không tìm thấy người dùng", 404);
        }

        user.IsActive = request.IsActive;

        if (!request.IsActive)
        {
            _tokenStore.RevokeUserTokens(user.Id);
        }

        await _userRepository.UpdateAsync(user, cancellationToken);

        var statusMessage = request.IsActive ? "Mở khóa tài khoản thành công" : "Khóa tài khoản thành công";
        return ApiResponse<UserDto>.Success(_mapper.Map<UserDto>(user), statusMessage);
    }

    public async Task<ApiResponse<UserDto>> UpdateUserAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        if (user == null)
        {
            return ApiResponse<UserDto>.Fail("Không tìm thấy người dùng", 404);
        }

        _mapper.Map(request, user);

        await _userRepository.UpdateAsync(user, cancellationToken);
        return ApiResponse<UserDto>.Success(_mapper.Map<UserDto>(user), "Cập nhật thông tin người dùng thành công");
    }

    public async Task<ApiResponse<bool>> DeleteUserAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        if (user == null)
        {
            return ApiResponse<bool>.Fail("Không tìm thấy người dùng", 404);
        }

        // Xóa cứng khỏi Database
        await _userRepository.DeleteAsync(user, cancellationToken);
        _tokenStore.RevokeUserTokens(user.Id);

        return ApiResponse<bool>.Success(true, "Xóa tài khoản người dùng thành công (xóa vĩnh viễn)");
    }

    public async Task<ApiResponse<IEnumerable<RoleDto>>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        var roles = await _roleRepository.GetAllAsync(cancellationToken);
        var dtos = _mapper.Map<IEnumerable<RoleDto>>(roles);

        return ApiResponse<IEnumerable<RoleDto>>.Success(dtos, "Lấy danh sách vai trò thành công");
    }
}
