using APICore.Common.Constants;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace APICore.Services.Impls
{
    public class InventorySettingsProvider : IInventorySettings
    {
        private const string CacheKey = "InventorySettings_Snapshot";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(2);

        private readonly ISettingService _settingService;
        private readonly IMemoryCache _cache;

        public InventorySettingsProvider(ISettingService settingService, IMemoryCache cache)
        {
            _settingService = settingService ?? throw new ArgumentNullException(nameof(settingService));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        public int RoundingDecimals => GetCached().RoundingDecimals;
        public int PriceRoundingDecimals => GetCached().PriceRoundingDecimals;
        public bool AllowNegativeStock => GetCached().AllowNegativeStock;
        public string DefaultUnitOfMeasure => GetCached().DefaultUnitOfMeasure;

        private InventorySettingsSnapshot GetCached()
        {
            return _cache.GetOrCreate(CacheKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheDuration;
                return LoadAsync().GetAwaiter().GetResult();
            });
        }

        private async Task<InventorySettingsSnapshot> LoadAsync()
        {
            var roundingStr = await _settingService.GetSettingOrDefaultAsync(SettingKeys.RoundingDecimals, SettingKeys.RoundingDecimalsDefault.ToString(CultureInfo.InvariantCulture));
            var priceRoundingStr = await _settingService.GetSettingOrDefaultAsync(SettingKeys.PriceRoundingDecimals, SettingKeys.PriceRoundingDecimalsDefault.ToString(CultureInfo.InvariantCulture));
            var allowNegativeStr = await _settingService.GetSettingOrDefaultAsync(SettingKeys.AllowNegativeStock, SettingKeys.AllowNegativeStockDefault.ToString(CultureInfo.InvariantCulture));
            var defaultUnit = await _settingService.GetSettingOrDefaultAsync(SettingKeys.DefaultUnitOfMeasure, SettingKeys.DefaultUnitOfMeasureDefault);

            return new InventorySettingsSnapshot
            {
                RoundingDecimals = ParseInt(roundingStr, SettingKeys.RoundingDecimalsDefault),
                PriceRoundingDecimals = ParseInt(priceRoundingStr, SettingKeys.PriceRoundingDecimalsDefault),
                AllowNegativeStock = ParseBool(allowNegativeStr, SettingKeys.AllowNegativeStockDefault),
                DefaultUnitOfMeasure = string.IsNullOrWhiteSpace(defaultUnit) ? SettingKeys.DefaultUnitOfMeasureDefault : defaultUnit.Trim()
            };
        }

        private static int ParseInt(string value, int defaultValue)
        {
            if (string.IsNullOrWhiteSpace(value)) return defaultValue;
            return int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : defaultValue;
        }

        private static bool ParseBool(string value, bool defaultValue)
        {
            if (string.IsNullOrWhiteSpace(value)) return defaultValue;
            var v = value.Trim().ToLowerInvariant();
            if (v == "1" || v == "true" || v == "yes") return true;
            if (v == "0" || v == "false" || v == "no") return false;
            return defaultValue;
        }

        /// <summary>
        /// Invalida la caché para que la próxima lectura recargue desde BD (útil tras PUT de settings).
        /// </summary>
        public void InvalidateCache()
        {
            _cache.Remove(CacheKey);
        }

        private class InventorySettingsSnapshot
        {
            public int RoundingDecimals { get; set; }
            public int PriceRoundingDecimals { get; set; }
            public bool AllowNegativeStock { get; set; }
            public string DefaultUnitOfMeasure { get; set; } = SettingKeys.DefaultUnitOfMeasureDefault;
        }
    }
}
