using Apps;
using System.Collections.Immutable;
using System.Diagnostics;
using PartsClass;
using System.Text.Json;

namespace Fetch.Builder;


public abstract class IFetchBuilder : IDisposable
{
    public Dictionary<string,string> ResponseValues = [];
    public IApp? app;
    protected JsonElement requestDoc;
    public int status = 200;

    public IFetchBuilder(JsonElement requestDocArg)
    {
        requestDoc = requestDocArg;
    }

    public virtual void AddValue(string key, string value)
    {
        ResponseValues.Add(key, value);
    }

    public abstract void BuildResponseValues();

    public string GetJSONString()
    {
        return JsonSerializer.Serialize(ResponseValues);
    }

    public void Return404()
    {
        status = 404;
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

public class FetchDirector : IDisposable
{
    public IFetchBuilder fetchBuilder;
    public Stopwatch stopWatch = new();

    readonly ImmutableDictionary<string, Type> types = ImmutableDictionary.CreateRange(
        [
            KeyValuePair.Create("loadPostPage", Type.GetType("Fetch.PostPage.LoadPostPage")!),
            KeyValuePair.Create("submitPostComment", Type.GetType("Fetch.PostComment.SubmitPostComment")!)
        ]);

    public FetchDirector(ref HttpRequest request)
    {
        if (!GetJSONFromRequest(ref request, out JsonElement requestDoc))
        {
            fetchBuilder = new Fetch404(requestDoc);
            return;
        }

        if (requestDoc.TryGetProperty("type", out JsonElement requestType))
        {
            string type = requestType.GetString()!;
            if (types.TryGetValue(type, out Type? fetchType))
            {
                stopWatch.Start();
                fetchBuilder = (IFetchBuilder)Activator.CreateInstance(fetchType, [requestDoc])!;
                return;
            }
        }
        fetchBuilder = new Fetch404(JsonDocument.Parse("{}").RootElement);
    }

    public void PrepJSONString()
    {
        fetchBuilder.BuildResponseValues();
        stopWatch.Stop();
        fetchBuilder.AddValue("buildTime", stopWatch.ToString());
    }

    public string GetJSONString()
    {
        return fetchBuilder.GetJSONString();
    }

    public bool GetJSONFromRequest(ref HttpRequest request, out JsonElement doc)
    {
        try
        {
            using var reader = new StreamReader(request.Body);
            string JSONString = reader.ReadToEndAsync().Result;
            doc = JsonDocument.Parse(JSONString).RootElement;
            return true;
        }
        catch
        {
            doc = JsonDocument.Parse("{}").RootElement;
            return false;
        }
    }

    public int GetStatus()
    {
        return fetchBuilder.status;
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
            fetchBuilder.Dispose();
        }
        _disposed = true;
    }
}


public class Fetch404(JsonElement requestArg) : IFetchBuilder(requestArg)
{
    public override void BuildResponseValues()
    {
        Return404();
    }
}