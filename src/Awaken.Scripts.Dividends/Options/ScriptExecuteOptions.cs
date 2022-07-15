using System;

namespace Awaken.Scripts.Dividends.Options;

public class ScriptExecuteOptions
{
    public string FirstExecutionTime { get; set; } = String.Empty;
    public int FixedTermSeconds { get; set; } = 24 * 60 * 60;
    public int ExecuteOffsetSeconds { get; set; } =  -1;
    public bool IsNewReward { get; set; } = false;
}