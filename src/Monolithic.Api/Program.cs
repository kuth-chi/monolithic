using Microsoft.EntityFrameworkCore;
using Monolithic.Api.Common.Extensions;
using Monolithic.Api.Modules.Identity;
using Monolithic.Api.Modules.Identity.Infrastructure.Data;
using Monolithic.Api.Modules.Platform;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApiServices(builder.Configuration, builder.Environment);

var app = builder.Build();

// ── Auto-apply pending EF Core migrations on every startup ───────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
}

app.UseApiPipeline(builder.Environment);

await app.InitializeIdentityAsync();
await app.InitializePlatformAsync();

app.Run();

public partial class Program;
