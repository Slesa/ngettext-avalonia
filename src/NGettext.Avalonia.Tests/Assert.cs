namespace Xunit;

public partial class AssertExt
{
    public static void DependsOn(string paramName, Action targetInvocation)
    {
        var e = Assert.Throws<ArgumentNullException>(targetInvocation);
        Assert.Equal(paramName, e.ParamName);
    }
}