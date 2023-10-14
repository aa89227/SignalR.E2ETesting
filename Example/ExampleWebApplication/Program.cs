using ExampleWebApplication.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
});
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});
var app = builder.Build();
app.UseResponseCompression();
app.MapHub<ExampleHub>("/examplehub");

app.Run();