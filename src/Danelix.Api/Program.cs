using Danelix.Modules.Identity.Infrastructure.Persistence;
using Danelix.Modules.Tenancy.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var databaseConnectionString = builder.Configuration.GetConnectionString("Danelix");
if (!string.IsNullOrWhiteSpace(databaseConnectionString))
{
    builder.Services.AddIdentityPersistence(databaseConnectionString);
    builder.Services.AddTenancyPersistence(databaseConnectionString);
}
else if (builder.Environment.IsDevelopment())
{
    throw new InvalidOperationException("ConnectionStrings:Danelix is required in Development.");
}

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await app.Services.InitializeIdentityDevelopmentAsync();
    await app.Services.InitializeTenancyDevelopmentAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

// Expuesta para que los tests de integración usen WebApplicationFactory<Program>.
public partial class Program { }
