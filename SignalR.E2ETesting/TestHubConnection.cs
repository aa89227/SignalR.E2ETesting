using Microsoft.AspNetCore.SignalR.Client;
using System.Collections.Concurrent;
using System.Reflection;

namespace SignalR.E2ETesting;

public class TestHubConnection<TResponses>
{
    private readonly HubConnection hubConnection;
    private readonly BlockingCollection<Message> Messages = new();

    public TResponses AssertThat { get; init; }

    public TestHubConnection(HubConnection hubConnection, int timeout = 1000)
    {
        this.hubConnection = hubConnection;
        AssertThat = HubAssertBuilder<TResponses>.Build(Messages);
        ListenAllMessages();
        hubConnection.StartAsync();
        TakeAndCompare.Timeout = timeout;
    }

    public static void Initial()
    {
        HubAssertBuilder<TResponses>.Initial();
    }

    /// <summary>
    /// Subscribes to all events of a SignalR hub client.
    /// </summary>
    internal void ListenAllMessages()
    {
        Type interfaceType = typeof(TResponses);
        MethodInfo[] methods = interfaceType.GetMethods();

        foreach (MethodInfo method in methods)
        {
            ParameterInfo[] parameters = method.GetParameters();

            Type[] parameterTypes = parameters.Select(x => x.ParameterType).ToArray();
            void handler(object?[] x)
            {
                Console.WriteLine($"Received message({hubConnection.ConnectionId}): {method.Name}");
                Messages.Add(new(method.Name, x!));
            }

            hubConnection.On(method.Name, parameterTypes, handler);
        }
    }

    public async Task SendAsync(string method, params object?[] args)
    {
        await hubConnection.SendCoreAsync(method, args);
    }
}

internal static class TestHubExtension
{
    public static IDisposable On(this HubConnection hubConnection, string methodName, Type[] parameterTypes, Action<object?[]> handler)
    {
        return hubConnection.On(methodName, parameterTypes, static (parameters, state) =>
        {
            var currentHandler = (Action<object?[]>)state;
            currentHandler(parameters);
            return Task.CompletedTask;
        }, handler);
    }
}