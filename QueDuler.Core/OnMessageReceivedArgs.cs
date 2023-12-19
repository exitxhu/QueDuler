
public record OnMessageReceivedArgs(string Message, string ConsumerId, string JobPath = null, object? originalMessage = null);

