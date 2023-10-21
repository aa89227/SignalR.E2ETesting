using ExampleWebApplication.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
});
var app = builder.Build();
app.MapHub<ExampleHub>("/examplehub");

app.Run();