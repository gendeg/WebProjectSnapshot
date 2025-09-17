using Microsoft.AspNetCore.Http;
using System.Net;
using WebServer;

namespace WebServerTests;

public class WebServerTests
{
    private RequestHandler handler;
    private HttpRequest request;
    private HttpResponse response;

    // setup
    public WebServerTests()
    {
        request = new DefaultHttpContext().Request;
        response = new DefaultHttpContext().Response;
        handler = new RequestHandler("home", request, ref response);
    }

    [Fact]
    public void HandlerForGet_Initiates()
    {
        Assert.IsType<RequestHandler>(handler);
    }

}