using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace HealthCheckApi.Services
{
    public class TokenService
    {
        private readonly IConfiguration _configuration;
        private readonly string _secretKey;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _expirationMinutes;

        public TokenService(IConfiguration Configuration)
        {
            _configuration = Configuration;
            var JwtSettings = _configuration.GetSection("JwtSettings");
            _secretkey = JwtSettings["key"] ?? throw new ArgumentNullException("JWT key not configured");
            _issuer = JwtSettings["Issuer"] ?? "HealthCheckApi";
            _audience = JwtSettings["Audience"] ?? "HealthCheckApiUsers";
            _expirationMinutes = int.Parse(JwtSettings["ExpiryMinutes"]?? "60");
        }

    /// <summary>
    /// Generate a JWT token for a user 
    /// </summary>
    /// <param name="userId">User's unique identifier</param>
    /// <param name="email">User's email</param>
    /// <returns>JWT token string</return>
    public string GenerateToken(string userId, string email, IEnumerable<string> roles = null)
    {
        var tokenHandler= new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_secretKey);

        var claims = new List<Claims>
        {
            new Claim(CLaimTypes.NameIdentifier, userId),
            new Claim(CLaimTypes.Email, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes. Integer64)
        };
        //Add roles
        if (roles != null )
        {
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
        }
        //describe token 
        var tokenDescriptor =  new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_expirationMinutes),
            SingingCredentials= new SingingCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
            Issuer= _issuer,
            Audience= _audience
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
        }

    /// <summary>
    /// Validates a JWT token and returns the claims principal
    /// </summary>
    /// <param name="token">JWT token to validate</param>
    /// <returns>ClaimsPrincipal if valid, null if invalid</returns>
    public ClaimsPrincipal ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_secretKey);

            var validationParameters = new TokenValidationParameters
            {
                validateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = true,
                CLockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
            return principal;
        }
        catch 
        {
            return null; 
        }
    }

    /// <summary>
    /// Extracts user ID from a JWT token
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <returns> User ID if found, null otherwise</returns>
    public string GetUserIdFromToken(string token)
    {
        var principal = ValidateToken(token);
        return principal?.FindFirst(CLaimTypes.NameIdentifier)?.Value;
    }

    /// <summary>
    /// Extracts email form token 
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <returns>Email if found, null otherwise</returns>
    public string GetEmailFromToken(string token)
    {
        var principal = ValidateToken(token);
        return principal?.FindFirst(ClaimTypes.Email)?.Value;
    }

    /// <summary>
    /// Extract roles from token
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <returns>List of roles</returns>
    public IEnumerable<string> GetRolesFromToken(string token)
    {
        var principal = ValidateToken(token);
        return principal?.FindAll(ClaimTypes.role)?.select(c => c.Value) ?? new List<string>();
    }

    /// <summary>
    /// Checks if a token has expired 
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <returns>True if expired, false otherwise</returns>
    public bool IsTokenExpired(string token)
    {
        try 
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jsonToken = tokenHandler.ReadJwtToken(token);
            return jsonToken.ValidTo < DateTime.UtcNow;
        }
        catch 
        {
            return true;
        }
    }
    }
}

