namespace CentralIdentity.Application.Services;

public sealed class TokenRequestException : Exception
{
    public TokenRequestException(string error, string description)
        : base(description)
    {
        Error = error;
        Description = description;
    }

    public string Error { get; }
    public string Description { get; }
}
