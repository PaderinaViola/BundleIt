using EShopService.Bundles.Application.Services;
using EShopService.Bundles.Application.Services;
using EShopService.Bundles.Infrastructure;
using EShopService.Bundles.Infrastructure.Persistence.Seeders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddScoped<IBundleService, BundleService>();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    var seeder = scope.ServiceProvider.GetRequiredService<IBundlesDataSeeder>();
    await seeder.SeedAsync();
}

app.UseHttpsRedirection();
app.MapControllers();
app.Run();