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
    }
}

