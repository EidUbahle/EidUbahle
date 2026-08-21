using CentralIdentity.Domain.Exceptions;
using Xunit;

namespace CentralIdentity.UnitTests.Domain;

public class ExceptionTests
{
    [Fact]
    public void NotFoundException_Has_Correct_Message()
    {
        var ex = new NotFoundException("User", Guid.Empty);

        Assert.Contains("User", ex.Message);
        Assert.Contains(Guid.Empty.ToString(), ex.Message);
    }

    [Fact]
    public void ValidationException_Stores_Errors()
    {
        var errors = new Dictionary<string, string[]>
        {
            { "Email", new[] { "Email is required", "Email is invalid" } }
        };

        var ex = new ValidationException(errors);

        Assert.True(ex.Errors.ContainsKey("Email"));
        Assert.Equal(2, ex.Errors["Email"].Length);
    }

    [Fact]
    public void ValidationException_Single_Field_Constructor_Works()
    {
        var ex = new ValidationException("Name", "Name is required");

        Assert.True(ex.Errors.ContainsKey("Name"));
        Assert.Single(ex.Errors["Name"]);
    }
}
