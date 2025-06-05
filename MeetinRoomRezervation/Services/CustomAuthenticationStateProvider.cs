using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;

namespace MeetinRoomRezervation.Services
{
    public class CustomAuthenticationStateProvider(IHttpContextAccessor httpContextAccessor, CookieService cookieService) : AuthenticationStateProvider
    {
        private ClaimsPrincipal _user = new ClaimsPrincipal(new ClaimsIdentity());

        public void MarkUserAsAuthenticated(string email, string role)
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, email),
                new Claim(ClaimTypes.Role, role)
            }, "CustomAuth");

            _user = new ClaimsPrincipal(identity);
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_user)));
        }

        public void MarkUserAsLoggedOut()
        {
            _user = new ClaimsPrincipal(new ClaimsIdentity());
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_user)));
        }

        public async override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            if (!_user.Claims.Any())
            {
                var token = httpContextAccessor?.HttpContext?.Request?.Cookies["Token"];
                if (token == null)
                {
                    token = await cookieService.GetFromCookieAsync("Token");
                }

                if (token != null)
                {
                    var handler = new JwtSecurityTokenHandler();
                    try
                    {
                        var jwtToken = handler.ReadJwtToken(token);

                        if (jwtToken.ValidTo < DateTime.UtcNow)
                        {
                            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                        }

                        var claims = jwtToken.Claims.ToList();
                        var claim = claims.FirstOrDefault(p => p.Type == "role");
                        claims.Remove(claim);
                        claims.Add(new Claim(ClaimTypes.Role, claim?.Value ?? "User"));
                        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        _user = new ClaimsPrincipal(identity);
                    }
                    catch (Exception ex)
                    {
                        _user = new ClaimsPrincipal(new ClaimsIdentity());
                        var exceptionJson = JsonSerializer.Serialize(new
                        {
                            ex.Message,
                            ex.StackTrace,
                            ex.Source,
                            InnerException = ex.InnerException?.Message
                        },
                            new JsonSerializerOptions
                            {
                                WriteIndented = true
                            }
                        );

                        Console.WriteLine(exceptionJson);
                    }
                }
            }
            var state = new AuthenticationState(_user);
            NotifyAuthenticationStateChanged(Task.FromResult(state));
            return state;
        }
    }
}