using CMS.Helpers;
using System.Configuration;

namespace KenticoCommunity.TinyPng;

/// <summary>
/// Gets TinyPng Settings from ConfigurationManager.
/// </summary>
public class TinyPngSettingsFromAppSettingsProvider : ITinyPngSettingsProvider
{
    public bool IsEnabled => ValidationHelper.GetBoolean(GetSetting(nameof(IsEnabled)), true);

    public string ApiKey => ValidationHelper.GetString(GetSetting(nameof(ApiKey)), string.Empty);

    public IEnumerable<string> AllowedExtensions => ValidationHelper.GetString(GetSetting(nameof(AllowedExtensions)), ".webp,.png,.jpg,.jpeg")
        .Split(new[] { ",", ";", "|", " " }, StringSplitOptions.RemoveEmptyEntries);

    private string GetSetting(string key)
    {
        return ConfigurationManager.AppSettings[$"TinyPng:{key}"];
    }
}
