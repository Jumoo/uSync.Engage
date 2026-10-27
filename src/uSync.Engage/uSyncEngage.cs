using uSync.BackOffice.Models;

namespace uSync.Engage;

public class uSyncEngage : ISyncAddOn
{
    public string Name => "uSync.Engage";

    public string Version => typeof(uSyncEngage).Assembly.GetName().Version?.ToString(3) ?? "17.0.0";

    public string Icon => "icon-target";

    public string View => string.Empty;

    public string Alias => "usyncEngage";

    public string DisplayName => "Engage";

    public int SortOrder => 110;
}
