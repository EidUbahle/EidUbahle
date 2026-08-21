using CentralIdentity.Domain.Common;
using Xunit;

namespace CentralIdentity.UnitTests.Domain;

public class ResultTests
{
    [Fact]
    public void Success_Result_IsSuccess_True()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Failure_Result_IsFailure_True()
    {
        const string error = "Something went wrong";
        var result = Result.Failure(error);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public void Generic_Success_Returns_Value()
    {
        var result = Result.Success(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Generic_Failure_Throws_On_Value_Access()
    {
        var result = Result.Failure<int>("error");

        Assert.False(result.IsSuccess);
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }
}
