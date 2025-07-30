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
                string token = null;

                if (httpContextAccessor?.HttpContext?.Request?.Cookies != null)
                {
                    token = httpContextAccessor.HttpContext.Request.Cookies["Token"];
                }

                if (string.IsNullOrEmpty(token))
                {
                    try
                    {
                        token = await cookieService.GetFromCookieAsync("Token");
                    }
                    catch (Exception)
                    {
                        token = null;
                    }
                }

                if (!string.IsNullOrEmpty(token))
                {
                    var handler = new JwtSecurityTokenHandler();
                    try
                    {
                        var jwtToken = handler.ReadJwtToken(token);

                        if (jwtToken.ValidTo < DateTime.UtcNow)
                        {
                            await ClearExpiredToken();
                            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                        }

                        var claims = jwtToken.Claims.ToList();
                        var roleClaim = claims.FirstOrDefault(p => p.Type == "role");
                        if (roleClaim != null)
                        {
                            claims.Remove(roleClaim);
                            claims.Add(new Claim(ClaimTypes.Role, roleClaim.Value));
                        }

                        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        _user = new ClaimsPrincipal(identity);

                        if (httpContextAccessor?.HttpContext != null)
                        {
                            httpContextAccessor.HttpContext.User = _user;
                        }
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

                        Console.WriteLine($"JWT Parse Error: {exceptionJson}");
                    }
                }
            }

            var state = new AuthenticationState(_user);
            return state;
        }

        private async Task ClearExpiredToken()
        {
            try
            {
                if (httpContextAccessor?.HttpContext?.Response != null)
                {
                    httpContextAccessor.HttpContext.Response.Cookies.Delete("Token");
                }
                await cookieService.RemoveFromCookieAsync("Token");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error clearing expired token: {ex.Message}");
            }
        }

        public void NotifyUserAuthentication()
        {
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        public void NotifyUserLoggedOut()
        {
            var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(anonymous)));
        }
    }
}