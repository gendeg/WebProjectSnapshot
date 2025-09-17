using Pages.Builder;
using Microsoft.AspNetCore.Http;

namespace BuilderTests;


public class PageDirectorTests
{

    private PageDirector director;

    // setup
    public PageDirectorTests()
    {
        HttpRequest request = new DefaultHttpContext().Request;
        director = new PageDirector("home", ref request);
    }
    [Fact]
    public void PageDirector_Initiates()
    {
        Assert.IsType<PageDirector>(director);
    }

    [Fact]
    public void PageDirector_MakesString()
    {
        Assert.IsType<string>(director.GetHTML());
    }
}