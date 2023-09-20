namespace ExampleWebApplication.Hubs;

public interface IExampleHubResponses
{
    Task Broadcast(string message);
    Task SendCollection(string[] values);
    Task SendObject(Data data);
}

public record Data(string Id, string Message);
