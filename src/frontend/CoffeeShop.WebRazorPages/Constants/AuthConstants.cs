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

    // App Routes
    public const string IndexRoute = "/Index";
    public const string LoginRoute = "/Auth/Login";
    public const string RegisterRoute = "/Auth/Register";
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

