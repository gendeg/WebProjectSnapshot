using Apps;
using System.Collections.Immutable;
using System.Text;
using System.Diagnostics;
using PartsClass;

namespace Pages.Builder;

/*
There are two main external references for IPageBuilder objects:

    Parts = get HTML pieces that make up the page (i.e. front end)
     - Ref: parts.cs for static Parts class code
     - Parts must be registered in Webserver/Parts/partPathRegister.json

    app = the interface with backend data stores to retrieve and
          format all required data to build the page (i.e. back end)
     - Ref: app.interface.cs for IApp interface code
     - ????? App data must be registerd in ?????
*/


public abstract class IPageBuilder : IDisposable
{
    public StringBuilder HTML = new();
    public Parts parts = new();
    public required IApp app;

    public abstract void BuildHead();

    public abstract void BuildBody();

    public abstract void ClosePage();

    public string GetHTML()
    {
        return HTML.ToString();
    }

    public void Return404()
    {
        HTML = new StringBuilder("__404__");
    }

    protected bool _disposed;
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }
        if (disposing)
        {
            app?.Dispose();
        }
        _disposed = true;
    }
}

public class PageDirector : IDisposable
{
    public IPageBuilder pageBuilder;
    public Stopwatch stopWatch = new();

    readonly ImmutableDictionary<string, Type> types = ImmutableDictionary.CreateRange(
        [
            KeyValuePair.Create("home", Type.GetType("Pages.HomePage.HomePageBuilder")!),
            KeyValuePair.Create("plaza", Type.GetType("Pages.Plaza.PlazaBuilder")!),
            KeyValuePair.Create("post", Type.GetType("Pages.Post.PostBuilder")!),
            KeyValuePair.Create("refresh", Type.GetType("Pages.Refresh.RefreshBuilder")!)
        ]);

    public PageDirector(string typeArg, ref HttpRequest request)
    {
        stopWatch.Start();
        Type pageType = types[typeArg];
        // TODO: implement whole page caching
        pageBuilder = (IPageBuilder)Activator.CreateInstance(pageType, [request])!;
    }

    public void BuildPage()
    {
        pageBuilder.BuildHead();
        pageBuilder.BuildBody();
        pageBuilder.ClosePage();
        stopWatch.Stop();
        pageBuilder.HTML.Append(stopWatch.ToString());
    }

    public string GetHTML()
    {
        return pageBuilder.GetHTML();
    }

    protected bool _disposed;
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }
        if (disposing)
        {
            pageBuilder.Dispose();
        }
        _disposed = true;
    }
}

