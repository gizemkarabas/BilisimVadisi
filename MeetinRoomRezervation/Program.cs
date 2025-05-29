using FluentValidation;
using MeetinRoomRezervation.Components;
using MeetinRoomRezervation.Data;
using MeetinRoomRezervation.Models;
using MeetinRoomRezervation.Services;
using MeetinRoomRezervation.Services.ReservationService;
using Microsoft.AspNetCore.Components.Authorization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
Log.Logger = new LoggerConfiguration()
	.MinimumLevel.Information()
	.WriteTo.Console()
	.WriteTo.File("Logs/app-.txt", rollingInterval: RollingInterval.Day)
	.CreateLogger();

builder.Host.UseSerilog();
builder.Services.AddHttpContextAccessor();
builder.Services.AddAuthentication()
	.AddCookie(options =>
	{
		options.LoginPath = "/login";
		options.LogoutPath = "/logout";
	});
builder.Services.AddAuthorization();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<IRoomService, RoomService>();

builder.Services.AddValidatorsFromAssemblyContaining<MeetingRoomValidator>();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
builder.Services.AddAuthorizationCore();
builder.Services.AddValidatorsFromAssemblyContaining<LoginModelValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<RegisterInputModelValidator>();
builder.Services.AddSingleton<IMongoClient>(serviceProvider =>
{
	var clientSettings = MongoClientSettings.FromConnectionString(builder.Configuration.GetConnectionString("MongoDb"));
	MongoClient client = new(clientSettings);
	ConventionPack conventionPack = [new IgnoreExtraElementsConvention(true)];
	ConventionRegistry.Register("MongoConventions", conventionPack, _ => true);
	return client;
});
builder.Services.AddSingleton<MongoDbContext>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddAntDesign();
builder.Services.AddScoped<ReservationDto>();

// Add services to the container.
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddAuthenticationCore();
builder.Services.AddScoped<SeedDataService>();
builder.Services.AddCascadingAuthenticationState();

//builder.Services.AddAuthentication(options =>
//{
//	options.DefaultScheme = IdentityConstants.ApplicationScheme;
//	options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
//})
//.AddCookie();

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

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
	.AddInteractiveServerRenderMode();

using (var scope = app.Services.CreateScope())
{
    var seedService = scope.ServiceProvider.GetRequiredService<SeedDataService>();
    await seedService.SeedAdminUserAsync();
}

await app.RunAsync();
