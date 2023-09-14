using Microsoft.AspNetCore.SignalR;

namespace ExampleWebApplication.Hubs;

public class ExampleHub : Hub
{
    public Task Broadcast(string message)
    {
        return Clients.All.SendAsync("Broadcast", message);
    }
}
