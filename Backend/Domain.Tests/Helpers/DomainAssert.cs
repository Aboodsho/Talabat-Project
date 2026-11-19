using Domain.Exceptions;
using Xunit;

namespace Domain.Tests.Helpers;

internal static class DomainAssert
{
    /// Asserts that the action throws a DomainException with the expected code.
    public static void Throws(string expectedCode, Action act)
    {
        var ex = Assert.Throws<DomainException>(act);
        Assert.Equal(expectedCode, ex.Code);
    }
}