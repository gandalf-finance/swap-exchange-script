using System;

namespace Awaken.Scripts.Dividends.Handlers.Events;

public class ToSwapTokenEvent
{
    public Guid Id { get; set; }
    public string TransactionId { get; set; }
}