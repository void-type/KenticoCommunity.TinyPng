namespace KenticoCommunity.TinyPng;

/// <summary>
/// Register an implementation of this interface to Kentico's DI to get settings from a custom location.
/// </summary>
public interface ITinyPngSettingsProvider
{
    /// <summary>
    /// When false, the module is bypassed.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Your API key. Get one from https://tinypng.com/developers.
    /// </summary>
    string ApiKey { get; }

    /// <summary>
    /// Collection of file extensions (eg: ".jpg", ".png") that the module will compress.
    /// </summary>
    IEnumerable<string> AllowedExtensions { get; }
}
