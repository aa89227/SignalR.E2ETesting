using System.Collections.Concurrent;

namespace SignalR.E2ETesting;

public class TakeAndCompare
{

    /// <summary>
    /// Assertion method that takes a BlockingCollection of `Message` objects and compares the method name and parameters to the given values.
    /// </summary>
    /// <param name="messages">A BlockingCollection of `Message` objects</param>
    /// <param name="methodName">The name of the method to be invoked</param>
    /// <param name="parameters">An array of objects that represent the method's parameters.</param>
    public static void Invoke(BlockingCollection<Message> messages, string methodName, object[] parameters)
    {
        CancellationTokenSource cancellationTokenSource = new(1000);
        var message = messages.Take(cancellationTokenSource.Token);
        if (message.MethodName != methodName)
        {
            throw new Exception();
        }
        if (!message.Parameters.SequenceEqual(parameters))
        {
            throw new Exception();
        }
    }
}
