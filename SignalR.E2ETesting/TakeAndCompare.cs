using System.Collections.Concurrent;
using System.Text.Json;

namespace SignalR.E2ETesting;

public class TakeAndCompare
{
    internal static int Timeout { get; set; }

    /// <summary>
    /// Assertion method that takes a BlockingCollection of `Message` objects and compares the method name and parameters to the given values.
    /// </summary>
    /// <param name="messages">A BlockingCollection of `Message` objects</param>
    /// <param name="methodName">The name of the method to be invoked</param>
    /// <param name="parameters">An array of objects that represent the method's parameters.</param>
    public static void Invoke(BlockingCollection<Message> messages, string methodName, object[] parameters)
    {
        CancellationTokenSource cancellationTokenSource = new(Timeout);
        try
        {
            var message = messages.Take(cancellationTokenSource.Token);
            if (message.MethodName != methodName)
            {
                throw new AssertFailedException($"SignalR assert failed. Method name not equal. Expected:<{parameters}>. Actual:<{message.MethodName}>. ");
            }
            var jsonParameters = JsonSerializer.Serialize(parameters);
            var jsonMessageParameters = JsonSerializer.Serialize(message.Parameters);
            if (jsonParameters != jsonMessageParameters)
            {
                throw new AssertFailedException($"SignalR assert failed. Parameters not equal. Expected:<{jsonParameters}>. Actual:<{jsonMessageParameters}>. ");
            }
        }
        catch (OperationCanceledException)
        {
            throw new OperationCanceledException($"Operation canceled. Timeout {Timeout} ms");
        }
        finally
        {
            cancellationTokenSource.Dispose();
        }
        
    }
    private class AssertFailedException : Exception
    {
        public AssertFailedException(string message) : base(message)
        {
        }
    }
}

