namespace MMOAS.AuthorityService.Application.Sessions;

public sealed class AuthoritySessionException : InvalidOperationException
{
    public AuthoritySessionException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public string Code { get; }
}
