namespace ExampleWebApplication.Hubs;

public interface IExampleHubResponses
{
    Task BroadcastMessage(string message);
}
