namespace CRM.BotService.Endpoint.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Models;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ViewModel;

public class AuthController(WebSettings webSettings) : Controller
{
    [AllowAnonymous]
    [HttpPost]
    [Route("api/authenticate")]
    public IActionResult Authenticate([FromBody] AuthViewModel auth)
    {
        if (IsValidCredential(auth))
        {
            var expiredAt = DateTimeOffset.Now.DateTime.AddDays(webSettings.ExpirationByDay);
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(webSettings.Secret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new([
                    new Claim(ClaimTypes.Name, auth.AppId)
                ]),

                Expires = expiredAt,
                SigningCredentials = new(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var securityToken = tokenHandler.CreateToken(tokenDescriptor);
            var token = tokenHandler.WriteToken(securityToken);

            return Ok(new { AccessToken = token, ExpriedDate = expiredAt });
        }

        return Unauthorized();
    }

    private bool IsValidCredential(AuthViewModel auth)
        => auth.AppId.Equals(webSettings.AppId) && Convert.ToBase64String(Encoding.UTF8.GetBytes(webSettings.AppToken)).Equals(auth.Token);
}