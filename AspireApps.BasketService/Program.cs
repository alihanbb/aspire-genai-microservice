using AspireApps.BasketService.ApiClients;
using AspireApps.BasketService.Endpoints;
using AspireApps.BasketService.Services;
using AspireApps.ServiceDefaults.Messaging;
using Keycloak.AuthServices.Authentication;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddRedisDistributedCache(connectionName: "redis-master");

builder.Services.AddScoped<BasketServices>();

builder.Services.AddHttpClient<CatalogApiClient>(client =>
{
    client.BaseAddress = new("https+http://aspireapps-catalogservice");
});
// Keycloak JWT Authentication
builder.Services.AddKeycloakWebApiAuthentication(builder.Configuration, options =>
{
    options.RequireHttpsMetadata = false;
    options.Audience = "account"; // Keycloak default audience
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuer = false, // Disable issuer validation due to dynamic ports
        ValidateAudience = true,
        ValidAudiences = new[] { "account", "basket-api" }
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("BasketAccess", policy =>
        policy.RequireAuthenticatedUser());
});
builder.Services.AddMasstransitWithAssemblies(Assembly.GetExecutingAssembly());

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapBasketEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
