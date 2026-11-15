using EShopService.Bundles.Application.Repositories;
using EShopService.Bundles.Application.Services;
using EShopService.Bundles.Presentation.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Register DI for Application and In-Memory infrastructure
builder.Services.AddSingleton<IBundleRepository, InMemoryBundleRepository>();
builder.Services.AddScoped<IBundleService, BundleService>();

var app = builder.Build();

app.UseHttpsRedirection();
app.MapControllers();
app.Run();