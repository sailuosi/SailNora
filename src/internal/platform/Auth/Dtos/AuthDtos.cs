namespace SailNora.Platform.Auth.Dtos;

public sealed class LoginInput
{
    public string UserName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}

public sealed class RefreshInput
{
    public string RefreshToken { get; set; } = string.Empty;
}

public sealed class LoginResult
{
    public string AccessToken { get; set; } = string.Empty;

    public string RefreshToken { get; set; } = string.Empty;

    public int ExpiresIn { get; set; }

    public string TokenType { get; set; } = "Bearer";
}

public sealed class UserInfoResult
{
    public string UserId { get; set; } = string.Empty;

    public string Account { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;

    public List<string> Permissions { get; set; } = [];
}
