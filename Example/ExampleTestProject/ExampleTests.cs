using Microsoft.AspNetCore.SignalR.Client;

namespace ExampleTestProject;

[TestClass]
public class ExampleTests
{
    [TestMethod]
    public async Task BroadcastTestAsync()
    {
        // Arrange
        var server = new TestServer();
        var clientA = server.CreateHubConnection("A");
        var clientB = server.CreateHubConnection("B");

        // Act
        await clientA.SendAsync("Broadcast", "Hello, World!");

        // Assert
        await clientA.AssertThat.Broadcast("Hello, World!");
        await clientB.AssertThat.Broadcast("Hello, World!");

    }
}