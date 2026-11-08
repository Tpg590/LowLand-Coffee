namespace CoffeeShop.WebRazorPages.Constants;

public static class AuthConstants
{
    // Named HttpClient được đăng ký trỏ về API Gateway
    public const string ApiGatewayClientName = "ApiGateway";

    // API Gateway Relative Endpoints (YARP reverse-proxy to Auth.API)
    public const string LoginEndpoint = "/api/auth/login";
    public const string RegisterEndpoint = "/api/auth/register";
    public const string RefreshTokenEndpoint = "/api/auth/refresh-token";
    public const string ProfileEndpoint = "/api/auth/profile";
    public const string ForgotPasswordEndpoint = "/api/auth/forgot-password";
    public const string ResetPasswordEndpoint = "/api/auth/reset-password";
    public const string ChangePasswordEndpoint = "/api/auth/change-password";
    public const string AvatarUploadEndpoint = "/api/auth/profile/avatar";
    public const string LogoutEndpoint = "/api/auth/logout";

    // API Gateway Relative Endpoints (YARP reverse-proxy to Auth.API - User Controller)
    public const string UsersEndpoint = "/api/users";
    public const string CreateStaffEndpoint = "/api/users/staff";
    public const string UserRolesEndpoint = "/api/users/roles";

    // App Routes
    public const string IndexRoute = "/Index";
    public const string LoginRoute = "/Auth/Login";
    public const string RegisterRoute = "/Auth/Register";
    public const string ForgotPasswordRoute = "/Auth/ForgotPassword";
    public const string ResetPasswordRoute = "/Auth/ResetPassword";
    public const string ProfileRoute = "/Auth/Profile";
    public const string LogoutRoute = "/Auth/Logout";
    public const string AdminUsersRoute = "/Admin/Users";
    public const string StaffOrdersRoute = "/Staff/Orders";

    // Roles
    public const string RoleAdmin = "Admin";
    public const string RoleStaff = "Staff";
    public const string RoleCustomer = "Customer";

    // Session & Cookie Claims
    public const string AccessTokenClaimType = "AccessToken";
    public const string RefreshTokenClaimType = "RefreshToken";
    public const string JwtSessionKey = "JWToken";
}

