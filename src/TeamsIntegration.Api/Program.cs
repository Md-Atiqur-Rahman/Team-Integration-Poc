using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.TokenCacheProviders.Distributed;
using MongoDB.Driver;
using TeamsIntegration.Api.Configuration;
using TeamsIntegration.Api.Models;
using TeamsIntegration.Api.Repositories;
using TeamsIntegration.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services
    .AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi(TeamsGraphService.RequiredScopes)
    .AddDistributedTokenCaches();

builder.Services.AddStackExchangeRedisCache(options =>
    builder.Configuration.GetSection("Redis").Bind(options));
builder.Services.Configure<MsalDistributedTokenCacheAdapterOptions>(options => options.Encrypt = true);
builder.Services.AddDataProtection()
    .SetApplicationName("TeamsIntegration.POC")
    .PersistKeysToFileSystem(new DirectoryInfo(
        Path.Combine(builder.Environment.ContentRootPath, "App_Data", "dataprotection-keys")));

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

builder.Services.Configure<MongoOptions>(builder.Configuration.GetSection("Mongo"));
builder.Services.AddSingleton<IMongoClient>(sp =>
    new MongoClient(sp.GetRequiredService<IOptions<MongoOptions>>().Value.ConnectionString));
builder.Services.AddSingleton(sp =>
{
    var options = sp.GetRequiredService<IOptions<MongoOptions>>().Value;
    return sp.GetRequiredService<IMongoClient>().GetDatabase(options.DatabaseName);
});
builder.Services.AddSingleton(sp =>
{
    var options = sp.GetRequiredService<IOptions<MongoOptions>>().Value;
    return sp.GetRequiredService<IMongoDatabase>()
        .GetCollection<TeamsConfiguration>(options.ConfigurationsCollectionName);
});
builder.Services.AddScoped<ITeamsConfigurationRepository, MongoTeamsConfigurationRepository>();
builder.Services.AddScoped<ITeamsConfigurationService, TeamsConfigurationService>();

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
