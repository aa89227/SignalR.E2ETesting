using Microsoft.AspNetCore.SignalR;

namespace ExampleWebApplication.Hubs;

public class ExampleHub : Hub<IExampleHubResponses>
{
    public async Task Broadcast(string message)
    {
        await Clients.All.Broadcast(message);
    }

    public async Task SendWithObject(string message)
    {
        await Clients.All.SendObject(new("1", message));
    }

    public async Task SendWithCollection(string message, string message2)
    {
        await Clients.All.SendCollection(new[] { message, message2 });
    }
}
