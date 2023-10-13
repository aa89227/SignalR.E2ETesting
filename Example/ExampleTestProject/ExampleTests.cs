using ExampleWebApplication.Hubs;

namespace ExampleTestProject;

[TestClass]
public class ExampleTests
{
    [AssemblyInitialize]
    public static void AssemblyInitialize(TestContext context)
    {
        // This method is called once before running the test assembly.
        TestHubConnection<IExampleHubResponses>.Initial();
    }

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

    [TestMethod]
    public async Task SendWithObjectTestAsync()
    {
        // Arrange
        var server = new TestServer();
        var clientA = server.CreateHubConnection("A");
        var clientB = server.CreateHubConnection("B");
        // Act
        await clientA.SendAsync("SendWithObject", "Hello, World!");
        // Assert
        await clientA.AssertThat.SendObject(new("1", "Hello, World!"));
        await clientB.AssertThat.SendObject(new("1", "Hello, World!"));
    }

    [TestMethod]
    public async Task SendWithCollectionTestAsync()
    {
        // Arrange
        var server = new TestServer();
        var clientA = server.CreateHubConnection("A");
        var clientB = server.CreateHubConnection("B");
        // Act
        await clientA.SendAsync("SendWithCollection", "Hello, World!", "Hello, World2!");
        // Assert
        await clientA.AssertThat.SendCollection(new[] { "Hello, World!", "Hello, World2!" });
        await clientB.AssertThat.SendCollection(new[] { "Hello, World!", "Hello, World2!" });
    }
}