# SignalR.E2ETesting
This repository is a framework for end-to-end testing of SignalR clients and servers.
Using interface to define the contract of the client and server, and using the framework to assert the contract is met.

## Features
- [x] Asserting value types of signalr messages.
- [] Asserting array types of signalr messages.
- [] Asserting reference types of signalr messages.

## How to use
Ensure the server and client are using the same contract.
You can use TypedClients for SignalR hub or not.
### Define the contract
```csharp
public interface IChatClient
{
    Task ReceiveMessage(string user, string message);
}
```
### Create the server
return the `TestHubConnection` with generic type of the contract
```csharp
internal class TestServer : WebApplicationFactory<Program>
{
    public TestHubConnection<IChatClient> CreateHubConnection(string userId)
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
```
### Test
This is a sample test for the above example.
`TestHubConnection` has a method `AssertThat` to assert the contract is met.
```csharp
[TestMethod]
public async Task TestMethod1()
{
    // Arrange
    var server = new TestServer();
    var hubConnection = server.CreateHubConnection("user1");

    // Act
    await hubConnection.SendAsync("SendMessage", "user2", "Hello");

    // Assert
    hubConnection.AssertThat.ReceiveMessage("user2", "Hello");
}
```