namespace Awaken.Scripts.Dividends.Options;

public class ScriptExecuteOptions
{
    public int FirstExecuteSeconds { get; set; } = 1000;
    public int FixedTermSeconds { get; set; } = 24 * 60 * 60;
    public int ExecuteOffsetSeconds = 1;
    public bool IsNewReward { get; set; } = false;
}