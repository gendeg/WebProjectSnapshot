using WebServer;
using System.Net;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.HttpLogging;
using AppSettings;
using Sessions;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Rewrite;

internal class Program
{
    private static void Main(string[] args)
    {
        LoadSettingsResourcesAndCaches();
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        ServerConfig(ref builder);
        WebApplication app = builder.Build();

        app.UseW3CLogging();
        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
        }
        app.UseHttpsRedirection();
        app.UseRewriter(new RewriteOptions()
            // remove trailing slashes from request URLs, necessary for consistent relative links
            .AddRedirect("^(.*)/(\\?.*)?$", "$1$2", StatusCodes.Status301MovedPermanently)
        );
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(
                Path.Combine(Settings.GetString("staticFileLocation")!))
        });
        app.UseSession();

        // typeArg must be registerd in PageDirector types dictionary
        app.Map("/", (HttpRequest request, HttpResponse response) => new RequestHandler("home", request, ref response).GetResult());
        app.Map("/fetch", (HttpRequest request, HttpResponse response) => new FetchHandler(request, ref response).GetResult());
        app.Map("/plaza/{plazaName}", (HttpRequest request, HttpResponse response) => new RequestHandler("plaza", request, ref response).GetResult());
        app.Map("/plaza/{plazaName}/style.css", (HttpRequest request, HttpResponse response) => new CSSHandler("plazaCSS", request, ref response).GetResult());
        app.Map("/plaza/{plazaName}/{postURL}/post", (HttpRequest request, HttpResponse response) => new RequestHandler("post", request, ref response).GetResult());
        if (app.Environment.IsDevelopment())
        {
            app.Map("/refreshCache", (HttpRequest request, HttpResponse response) => new RequestHandler("refresh", request, ref response).GetResult());
        }
        
        app.Use(async (context, next) =>
        {
            PrePageLoadActions(ref context);
            await next(context);
        });
        
        app.Run();
    }


    private static void LoadSettingsResourcesAndCaches()
    {
        AppSettings.Settings.Get();
        PartsClass.PartCache.Get();
        Apps.DataRegister.Initialize();
        BranchRegistration.BranchRegister.Get();
        DB.PostgreSQL.PgSources.Get();
        Cache.PersistentCache.Get();
        Cache.DisplayNameLookup.Initialize();
    }

    private static void PrePageLoadActions(ref HttpContext context)
    {
        context.Items["currentSession"] = new CurrentSession(ref context);
        Authentication.ThisContext.Initialize(context);
    }

    private static void ServerConfig(ref WebApplicationBuilder builder)
    {
        builder.WebHost.ConfigureKestrel((context, options) =>
        {
            options.ListenAnyIP(443, listenOptions =>
            {
                listenOptions.Protocols = HttpProtocols.Http1AndHttp2AndHttp3;
                listenOptions.UseHttps();
            });
            options.ListenAnyIP(80, listenOptions =>
            {
                listenOptions.Protocols = HttpProtocols.Http1AndHttp2AndHttp3;
            });
        });

        builder.Services.AddHttpsRedirection(options =>
        {
            options.RedirectStatusCode = (int)HttpStatusCode.TemporaryRedirect;
            options.HttpsPort = 443;
        });

        // TODO-Long-Term: add real distributed cache
        builder.Services.AddDistributedMemoryCache();

        builder.Services.AddSession();

        builder.Services.AddAuthentication().AddCookie(options =>
        {
            options.Cookie.Name = "Plaza";
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
            options.ExpireTimeSpan = TimeSpan.FromSeconds(1200);
        });

        builder.Services.AddLogging(options =>
        {
            options.AddProvider(new Logging.FileLogProvider(
                new StreamWriter(Settings.GetString("logDirectory") + Settings.GetString("logFile"), append: true)));
        });

        builder.Services.AddW3CLogging(logging =>
        {
            logging.LoggingFields =
                W3CLoggingFields.Request |
                W3CLoggingFields.TimeTaken |
                W3CLoggingFields.Date |
                W3CLoggingFields.Time |
                W3CLoggingFields.ClientIpAddress |
                W3CLoggingFields.UserAgent;
            logging.FileSizeLimit = 5 * 1024 * 1024;
            logging.RetainedFileCountLimit = 5;
            logging.FileName = "WebLog";
            logging.LogDirectory = Settings.GetString("logDirectory")!;
            logging.FlushInterval = TimeSpan.FromSeconds(2);
        });
    }
}