using Microsoft.JSInterop;

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
            catch
            {
                return null;
            }
        }

        public async Task RemoveFromCookieAsync(string key)
        {
            await _jsRuntime.InvokeVoidAsync(
                "cookieManager.removeCookie",
                key
            );
        }

        public async Task RemoveTokenFromCookieAsync()
        {
            await RemoveFromCookieAsync(TokenKey);
        }
    }
}
