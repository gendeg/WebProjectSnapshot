using BranchRegistration;

namespace BranchRegistrationTests;

public class BranchRegisterTests
{
    
    [Fact]
    public void Test_BranchRegister()
    {
        string result = BranchRegister.Get().Lookup("test");
        Assert.Equal("True", result);
    }
}