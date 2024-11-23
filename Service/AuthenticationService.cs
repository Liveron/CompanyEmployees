using Entities.Models;
using LoggerService;
using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Shared.DataTransferObjects;
using Service.Contracts;
using Entities.Exceptions;
using Entities.ConfigurationModels;
using Microsoft.Extensions.Options;

namespace Service;

internal sealed class AuthenticationService : IAuthenticationService
{
    private readonly ILoggerManager _logger;
    private readonly UserManager<User> _userManager;
    private readonly IOptions<JwtConfiguration> _configuration;
    private readonly JwtConfiguration _jwtConfiguration;

    private User? _user;

    public AuthenticationService(ILoggerManager logger, 
        UserManager<User> userManager, IOptions<JwtConfiguration> configuration)
    {
        _logger = logger;
        _userManager = userManager;
        _configuration = configuration;
        _jwtConfiguration = _configuration.Value;
    }

    public async Task<TokenDto> CreateToken(bool populateExp)
    {
        SigningCredentials sigingCredentials = GetSigningCredentials();
        Dictionary<string, object> claims = await GetClaims();
        SecurityTokenDescriptor tokenOptions = GenerateTokenOptions(sigingCredentials, claims);

        string refreshToken = GenerateRefreshToken();

        _user.RefreshToken = refreshToken;

        if (populateExp)
            _user.RefreshTokenExpiryTime = DateTime.Now.AddDays(7).ToUniversalTime();

        await _userManager.UpdateAsync(_user);

        string accessToken = new JsonWebTokenHandler().CreateToken(tokenOptions);

        return new TokenDto(accessToken, refreshToken);
    }

    public async Task<IdentityResult> RegisterUser(UserForRegistrationDto userForRegistration)
    {
        User user = userForRegistration.Adapt<User>();

        IdentityResult result = await _userManager.CreateAsync(user, userForRegistration.Password);

        if (result.Succeeded)
            await _userManager.AddToRolesAsync(user, userForRegistration.Roles);

        return result;
    }

    public async Task<bool> ValidateUser(UserForAuthenticationDto userForAuth)
    {
        _user = await _userManager.FindByNameAsync(userForAuth.UserName);

        bool result = _user is not null && await _userManager.CheckPasswordAsync(
            _user, userForAuth.Password);

        if (!result)
        {
            _logger.LogWarning($"{nameof(ValidateUser): Authentication failed. Wrong user name or password.}");
        }

        return result;
    }

    public async Task<TokenDto> RefreshToken(TokenDto tokenDto)
    {
        ClaimsIdentity identity = await GetIdentityFromExpiredToken(tokenDto.AccessToken);

        User? user = await _userManager.FindByNameAsync(identity.Name);
        if (user is null || user.RefreshToken != tokenDto.RefreshToken
            || user.RefreshTokenExpiryTime <= DateTime.Now)
            throw new RefreshTokenBadRequest();

        _user = user;

        return await CreateToken(populateExp: false);
    }

    private static SigningCredentials GetSigningCredentials()
    {
        byte[] key = Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("SECRET")!);
        var secret = new SymmetricSecurityKey(key);

        return new SigningCredentials(secret, SecurityAlgorithms.HmacSha256);
    }

    private async Task<Dictionary<string, object>> GetClaims()
    {
        var claims = new Dictionary<string, object>
        {
            { ClaimTypes.Name, _user.UserName },
        };

        IList<string> roles = await _userManager.GetRolesAsync(_user);
        foreach (var role in roles)
        {
            claims.Add(ClaimTypes.Role, role);
        }

        return claims;
    }

    private SecurityTokenDescriptor GenerateTokenOptions(SigningCredentials signingCredentials,
        Dictionary<string, object> claims)
    {
        var tokenOptions = new SecurityTokenDescriptor
        {
            Issuer = _jwtConfiguration.ValidIssuer,
            Audience = _jwtConfiguration.ValidAudience,
            Claims = claims,
            Expires = DateTime.Now.AddMinutes(Convert.ToDouble(_jwtConfiguration.Exprires)),
            SigningCredentials = signingCredentials,
        };

        return tokenOptions; 
    }

    private static string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    private async Task<ClaimsIdentity> GetIdentityFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidateIssuer = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("SECRET"))),
            ValidateLifetime = true,
            ValidIssuer = _jwtConfiguration.ValidIssuer,
            ValidAudience = _jwtConfiguration.ValidAudience,
        };

        var tokenHandler = new JsonWebTokenHandler();

        TokenValidationResult result = await tokenHandler.ValidateTokenAsync(
            token, tokenValidationParameters);

        if (!result.IsValid)
            throw result.Exception;

        return result.ClaimsIdentity;
    }
}
