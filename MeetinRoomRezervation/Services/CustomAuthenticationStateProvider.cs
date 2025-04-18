using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
	private ClaimsPrincipal _user = new ClaimsPrincipal(new ClaimsIdentity());

	public void MarkUserAsAuthenticated(string email)
	{
		var identity = new ClaimsIdentity(new[]
		{
			new Claim(ClaimTypes.Name, email),
			new Claim(ClaimTypes.Role, "User") 
        }, "CustomAuth");

		_user = new ClaimsPrincipal(identity);
		NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_user)));
	}

	public void MarkUserAsLoggedOut()
	{
		_user = new ClaimsPrincipal(new ClaimsIdentity());
		NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_user)));
	}

	public override Task<AuthenticationState> GetAuthenticationStateAsync()
	{
		return Task.FromResult(new AuthenticationState(_user));
	}
}
