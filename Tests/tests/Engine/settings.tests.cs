using AppSettings;

namespace SettingsTests;

public class SettingsTests
{

    private Settings settings;

    // setup
    public SettingsTests()
    {
        settings = Settings.Get();
    }
    
    [Fact]
    public void CheckRootPath()
    {
        Assert.Equal("S://Webapp/App/", settings.rootPath);
    }

    [Fact]
    public void LookupSettings()
    {
        Assert.NotNull(Settings.GetString("pgRDBMSConnection"));
        Assert.Null(Settings.GetString("notarealsetting"));
    }
}