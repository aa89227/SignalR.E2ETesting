using ExampleWebApplication.Hubs;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;

namespace ExampleTestProject;

internal class TestServer : WebApplicationFactory<Program>
{
    public TestHubConnection<IExampleHubResponses> CreateHubConnection(string userId)
    {
        var hubConnection = new HubConnectionBuilder()
            .WithUrl($"{Server.BaseAddress}examplehub?userId={userId}", options =>
            {
                options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.ServerSentEvents;
                options.HttpMessageHandlerFactory = _ => Server.CreateHandler();
            })
            .Build();

        return new TestHubConnection<IExampleHubResponses>(hubConnection);
    }
}