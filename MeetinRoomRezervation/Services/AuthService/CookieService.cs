using Microsoft.JSInterop;
using System.Text.Json;

namespace MeetinRoomRezervation.Services
{
    public class CookieService
    {
        private readonly IJSRuntime _jsRuntime;
        private const string TokenKey = "Token";

        public CookieService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task StoreInCookieAsync(string token, string key, int expirationDays = 7)
        {
            await _jsRuntime.InvokeVoidAsync(
                "cookieManager.setCookie",
                key,
                token,
                expirationDays
            );
        }

        public async Task<string> GetFromCookieAsync(string key)
        {
            try
            {
                return await _jsRuntime.InvokeAsync<string>(
                    "cookieManager.getCookie",
                    key
                );
            }
            catch (Exception ex)
            {
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
                return null;
            }
        }

        public async Task RemoveTokenFromCookieAsync()
        {
            await _jsRuntime.InvokeVoidAsync(
                "cookieManager.removeCookie",
                TokenKey
            );
        }
    }
}
