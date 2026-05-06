namespace CampusConnect.API.Common;

internal static class AuthSchemes
{
    public const string Combined = "CampusConnect.Combined";
    public const string Browser = "CampusConnect.Browser";
    public const string BrowserCookieName = "CampusConnect.Auth";

    public static readonly TimeSpan IdleTimeout = TimeSpan.FromMinutes(15);
}
