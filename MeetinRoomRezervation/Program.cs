using FluentValidation;
using MeetinRoomRezervation.Components;
using MeetinRoomRezervation.Models;
using MeetinRoomRezervation.Services;
using MeetinRoomRezervation.Services.ReservationService;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddScoped<IReservationService, ReservationService>();

builder.Services.AddValidatorsFromAssemblyContaining<MeetingRoomValidator>();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
builder.Services.AddAuthorizationCore();
builder.Services.AddValidatorsFromAssemblyContaining<LoginModelValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<RegisterInputModelValidator>();
builder.Services.AddSingleton<MongoDbContext>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddAntDesign();
builder.Services.AddScoped<ReservationDto>();

// Add services to the container.
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddAuthenticationCore();

builder.Services.AddCascadingAuthenticationState();

builder.Services.AddAuthentication(options =>
{
	options.DefaultScheme = IdentityConstants.ApplicationScheme;
	options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
})
.AddCookie();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseMigrationsEndPoint();
}
else
{
	app.UseExceptionHandler("/Error", createScopeForErrors: true);
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
	.AddInteractiveServerRenderMode();

app.Run();
