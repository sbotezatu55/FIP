using Fip.Application;
using Fip.Identity;
using Fip.Infrastructure;
using Fip.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddControllers();
builder.Services.AddInfrastructure();
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddIdentity();

var app = builder.Build();

app.MapControllers();
app.Run();
