using Sitecore.Configuration;
using Sitecore.Diagnostics;
using Sitecore.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace Sitecore.Support.Web
{
  public static class WebEditUtil
  {
    private readonly static Regex _serverUrlRegex = new Regex(
        @"<[^>]+?=""(?<serverurl>http(s?)\://[a-z0-9.-]+(:[0-9]+)?)(.+?)\""",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static class PrefixesRegexRunner
    {
      public static RegexParameters[] UrlPrefixParameters;

      static PrefixesRegexRunner()
      {
        var prefixes = new string[]
                  {
                    Constants.LinkPrefix,
                    Constants.MediaRequestPrefix
                  };

        UrlPrefixParameters = new RegexParameters[prefixes.Length];

        for (int i = 0; i < prefixes.Length; i++)
        {
          var localPrefix = prefixes[i];
          var escapedPrefix = Regex.Escape(localPrefix);

          UrlPrefixParameters[i] = new RegexParameters
          {
            Regex = new Regex(string.Format(@"(<[^>]+?=""/{0})", escapedPrefix), RegexOptions.IgnoreCase | RegexOptions.Compiled),
            Pattern = "/" + escapedPrefix,
            Replacement = localPrefix
          };
        }
      }

      public static string RunEachRegex(string value)
      {
        foreach (var urlPrefixParameter in UrlPrefixParameters)
        {
          value = urlPrefixParameter.RunRegex(value);
        }

        return value;
      }
    }

    private class RegexParameters
    {
      public Regex Regex { get; set; }

      public string Pattern { get; set; }

      public string Replacement { get; set; }

      public string RunRegex(string value)
      {
        return this.Regex.Replace(value, match => Regex.Replace(match.Value, this.Pattern, this.Replacement));
      }
    }

    private static readonly string FirefoxItemLinkPrefix = "~/link.aspx?".Replace("~", "%7E");

    public static string RepairLinks(string value)
    {
      Assert.ArgumentNotNull(value, "value");

      if (UIUtil.IsFirefox())
      {
        var fireFoxMediaLinkPrefix = Settings.Media.DefaultMediaPrefix.Replace("~", "%7E");
        value = value.Replace(FirefoxItemLinkPrefix, Constants.LinkPrefix).Replace(fireFoxMediaLinkPrefix, Settings.Media.DefaultMediaPrefix);
      }

      var url = HttpContext.Current.Request.Url;
      var slashIndex = url.AbsolutePath.LastIndexOf('/');

      if (slashIndex < 0)
      {
        return value;
      }

      var serverUrl = WebUtil.GetServerUrl(url, false);
      var currentPath = url.AbsolutePath.Substring(0, slashIndex);
      
      value = _serverUrlRegex.Replace(
              value,
              match =>
              {
                var matchUrl = match.Groups["serverurl"].Value;

                var result = match.Value;

                if (matchUrl.StartsWith(serverUrl, StringComparison.OrdinalIgnoreCase))
                {
                  var index = match.Value.IndexOf(serverUrl, StringComparison.OrdinalIgnoreCase);
                  result = match.Value.Remove(index, matchUrl.Length);
                }

                result = result.Replace(currentPath, string.Empty);

                return result;
              });

      value = PrefixesRegexRunner.RunEachRegex(value);

      return value;
    }
  }
}