using Microsoft.AspNetCore.SignalR.Client;
using System.Collections.Concurrent;

namespace SignalR.E2ETesting;

public class TestHubConnection<TResponses>
{
    private readonly HubConnection hubConnection;
    private readonly BlockingCollection<MethodAndParam> methodAndParams = new();

    public TResponses AssertThat { get; init; }
    public TestHubConnection(HubConnection hubConnection)
    {
        this.hubConnection = hubConnection;
        AssertThat = HubAssertBuilder<TResponses>.Build(methodAndParams);
        // TODO: Listen All Events
        hubConnection.StartAsync();
    }

    public async Task SendAsync(string method, params object?[] args)
    {
        await hubConnection.SendCoreAsync(method, args);
    }
}