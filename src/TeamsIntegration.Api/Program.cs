using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Identity.Web;
using TeamsIntegration.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services
    .AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi(TeamsGraphService.RequiredScopes)
    .AddInMemoryTokenCaches();

builder.Services.Configure<CookieAuthenticationOptions>(
    CookieAuthenticationDefaults.AuthenticationScheme,
    options =>
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

builder.Services.AddAuthorization();
builder.Services.AddHttpClient<ITeamsGraphService, TeamsGraphService>(client =>
{
    client.BaseAddress = new Uri("https://graph.microsoft.com/v1.0/");
});
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Teams Integration POC API",
        Version = "v1",
        Description = "Open /api/auth/connect in a browser tab before using protected endpoints. "
            + "Swagger sends the resulting same-origin secure session cookie automatically."
    });
});

var app = builder.Build();

app.UseExceptionHandler(errorApp => errorApp.Run(TeamsGraphExceptionHandler.HandleAsync));

app.UseHttpsRedirection();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;
