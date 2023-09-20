namespace ExampleWebApplication.Hubs;

public interface IExampleHubResponses
{
    Task Broadcast(string message);
}
