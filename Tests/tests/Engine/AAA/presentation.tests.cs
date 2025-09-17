using Presentation;
using Utilities;


namespace PresentationTests;

public class PresTests
{
    [Fact]
    public void Test_RemoveNonAlphaNum()
    {
        Assert.Equal("qwerty", Pres.RemoveNonAlphaNum("qwerty"));
        Assert.Equal("qwERtY", Pres.RemoveNonAlphaNum("qwERtY"));
        Assert.Equal("qwerty123", Pres.RemoveNonAlphaNum("qwerty123"));
        Assert.Equal("123", Pres.RemoveNonAlphaNum("123"));
        Assert.Equal("", Pres.RemoveNonAlphaNum("^%*&^%&^>?~"));
        Assert.Equal("qwerty", Pres.RemoveNonAlphaNum("qwerty!"));
        Assert.Equal("qwertytryingtohack", Pres.RemoveNonAlphaNum(".qwerty!<trying_to_hack>"));
        Assert.Equal("", Pres.RemoveNonAlphaNum(""));
        Assert.Equal("qwertytryingtohack", Pres.RemoveNonAlphaNum(".qwerty!\n<trying_\nto_hack>"));
    }

    [Fact]
    public void Test_ReplaceGreaterThanLessThan()
    {
        Assert.Equal("&lt; &gt;", Pres.ReplaceGreaterThanLessThan("< >"));
        Assert.Equal("&lt;&lt;&lt; &gt;&gt;&gt;", Pres.ReplaceGreaterThanLessThan("<<< >>>"));
        Assert.Equal("&lt;script src='bad.js'&gt;&lt;/script&gt;", Pres.ReplaceGreaterThanLessThan("<script src='bad.js'></script>"));
        Assert.Equal("all good here!", Pres.ReplaceGreaterThanLessThan("all good here!"));
        Assert.Equal("&lt;script src='bad.js'&gt;\n&lt;/script&gt;", Pres.ReplaceGreaterThanLessThan("<script src='bad.js'>\n</script>"));
    }

    [Fact]
    public void Test_MakeHTMLSafe()
    {
        Assert.Equal("&lt; &gt;", Pres.MakeHTMLSafe("< >"));
        Assert.Equal("&lt;&lt;&lt; &gt;&gt;&gt;", Pres.MakeHTMLSafe("<<< >>>"));
        Assert.Equal("&lt;script src='bad.js'&gt;&lt;/script&gt;", Pres.MakeHTMLSafe("<script src='bad.js'></script>"));
        Assert.Equal("all good here!", Pres.MakeHTMLSafe("all good here!"));
        Assert.Equal("&lt;script src='bad.js'&gt;\n&lt;/script&gt;", Pres.MakeHTMLSafe("<script src='bad.js'>\n</script>"));
    }

    [Fact]
    public void Test_MakeHTMLAttributeSafe()
    {
        Assert.Equal("&lt; &quot; &apos; &amp; &gt;", Pres.MakeHTMLAttributeSafe("< \" ' & >"));
    }

    [Fact]
    public void Test_ValidateEmail()
    {
        Assert.True(Pres.ValidateEmail("gary@garygende.com"));
        Assert.True(Pres.ValidateEmail("gary.hey@garygende.com.com.com"));
        Assert.True(Pres.ValidateEmail("gary_%+-@garygende.com"));
        Assert.True(Pres.ValidateEmail("gary@reallyreallylongdomainbutseriouslythoughwhywoulditbethisfreakinlong.com"));
        Assert.False(Pres.ValidateEmail("gary@garygende"));
        Assert.False(Pres.ValidateEmail("gary@garygende..com"));
        Assert.False(Pres.ValidateEmail("megasuperlongemailthatexceedsmaxsize_____________________________________________________________________________________________________________________________________________________@short.reallyreallylongdomainbutseriouslythoughwhywoulditbethislongcom"));
    }

    [Fact]
    public void Test_IDToURLToID()
    {
        UInt128 num;
        string url;
        for (int i = 0; i < 5000; i++)
        {
            num = Utils.CreateItemID();
            url = Pres.IDToURL(num);
            Assert.Equal(num, Pres.URLToID(url));
        }
    }

}