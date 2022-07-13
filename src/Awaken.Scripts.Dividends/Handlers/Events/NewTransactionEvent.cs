namespace Awaken.Scripts.Dividends.Handlers.Events;

public class NewTransactionEvent
{
    public string TransactionId { get; set; }
    public string ToAddress { get; set; }
    public string MethodName { get; set; }
    public string Message { get; set; }
}