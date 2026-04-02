using Microsoft.Extensions.Localization;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace APICore.API.Utils.JsonLocalization
{
    public class JsonStringLocalizer : IStringLocalizer
    {
        private List<JsonLocalization> localization = new List<JsonLocalization>();

        public JsonStringLocalizer()
        {
            JsonSerializer serializer = new JsonSerializer();
            localization = JsonConvert.DeserializeObject<List<JsonLocalization>>(File.ReadAllText(@"i18n/localization.json"))
                ?? new List<JsonLocalization>();
        }

        public LocalizedString this[string name]
        {
            get
            {
                var value = GetString(name);
                return new LocalizedString(name, value ?? name, resourceNotFound: value == null);
            }
        }

        public LocalizedString this[string name, params object[] arguments]
        {
            get
            {
                var format = GetString(name);
                var value = string.Format(format ?? name, arguments);
                return new LocalizedString(name, value, resourceNotFound: format == null);
            }
        }

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            var culture = CultureInfo.CurrentCulture.Name;
            return localization
                .Where(l => l?.LocalizedValue != null && l.LocalizedValue.ContainsKey(culture))
                .Select(l => new LocalizedString(l.Key, l.LocalizedValue[culture], true));
        }

        public IStringLocalizer WithCulture(CultureInfo culture)
        {
            return new JsonStringLocalizer();
        }

        private string GetString(string name)
        {
            if (string.IsNullOrEmpty(name) || localization == null)
                return null;
            var culture = CultureInfo.CurrentCulture.Name;
            var value = localization.FirstOrDefault(l =>
                l != null
                && l.Key == name
                && l.LocalizedValue != null
                && l.LocalizedValue.ContainsKey(culture));
            if (value?.LocalizedValue == null || !value.LocalizedValue.TryGetValue(culture, out var text))
                return null;
            return text;
        }
    }
}