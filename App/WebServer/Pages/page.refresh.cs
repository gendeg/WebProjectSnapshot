using Pages.Builder;
using PartsCommon;

namespace Pages.Refresh;


public class RefreshBuilder : IPageBuilder
{
    public RefreshBuilder(ref HttpRequest requestArg)
    {
    }

    public override void BuildHead()
    {
        HTML.Append(CommonParts.HTMLDocStart);
        HTML.Append("<title>Cache Refresh</title>\n");
        HTML.Append(CommonParts.HeadClose);
    }

    public override void BuildBody()
    {
        AppSettings.Settings.Refresh();
        PartsClass.PartCache.Refresh();
        Apps.DataRegister.Refresh();
        BranchRegistration.BranchRegister.Refresh();
        Cache.PersistentCache.Refresh();
        Cache.DisplayNameLookup.Initialize();
        HTML.Append("Caches have been refreshed!");
    }

    public override void ClosePage()
    {
        HTML.Append(CommonParts.HTMLDocEnd);
    }
}