using Apps;
using PartsClass;

namespace PartTests;

public class TestApp : BlankApp
{
    public TestApp()
    {
        SetValLocal("one", "value_one");
        SetValLocal("two", "value_two");
    }
}

public class PartTests
{
    TestApp app = new();
    Parts parts = new();

    [Fact]
    public void Parts_CanGet()
    {
        Assert.Equal("stuff\r\nmore stuff", parts.Get("test", app));
    }

    [Fact]
    public void Parts_CanParseParts()
    {
        Assert.Equal("__stuff\r\nmore stuff__stuff\r\nmore stuff__", parts.Get("test2", app));
    }

    [Fact]
    public void Parts_CanParseValues()
    {
        Assert.Equal("__value_one__value_two__", parts.Get("test3", app));
    }

}