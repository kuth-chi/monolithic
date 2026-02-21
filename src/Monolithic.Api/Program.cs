using Monolithic.Api.Common.Extensions;
using Monolithic.Api.Modules.Identity;
using Monolithic.Api.Modules.Platform;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApiServices(builder.Configuration, builder.Environment);

var app = builder.Build();

app.UseApiPipeline(builder.Environment);

await app.InitializeIdentityAsync();
await app.InitializePlatformAsync();

app.Run();

public partial class Program;
