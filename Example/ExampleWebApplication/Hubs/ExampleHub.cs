using Microsoft.AspNetCore.SignalR;

namespace ExampleWebApplication.Hubs;

public class ExampleHub : Hub<IExampleHubResponses>
{
    public async Task Broadcast(string message)
    {
        await Clients.All.Broadcast(message);
    }
}
