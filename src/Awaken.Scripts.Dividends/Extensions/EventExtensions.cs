using AElf.CSharp.Core;
using AElf.Types;
using Google.Protobuf;

namespace Awaken.Scripts.Dividends.Extensions;

public static class EventExtensions
{
    public static void MergeFrom<T>(this T eventData, LogEvent log) where T : IEvent<T>
    {
        foreach (var bs in log.Indexed)
        {
            eventData.MergeFrom(bs);
        }

        eventData.MergeFrom(log.NonIndexed);
    }
}