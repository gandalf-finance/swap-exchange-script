namespace Awaken.Scripts.Dividends.Options;

public class ScriptExecuteOptions
{
    public int FirstExecute { get; set; } = 1000;
    public int FixedTerm { get; set; } = 24 * 60 * 60;
    public int ExecuteOffset = 1;
    public bool IsNewReward { get; set; } = false;
}