using APICore.API.Authorization;
using APICore.API.BasicResponses;
using APICore.Common.Constants;
using APICore.Common.DTO.Request;
using APICore.Common.DTO.Response;
using APICore.Services;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace APICore.API.Controllers
{
    [Route("api/setting")]
    public class SettingController : Controller
    {
        private readonly ISettingService _settingService;
        private readonly IInventorySettings _inventorySettings;
        private readonly IMapper _mapper;

        public SettingController(ISettingService settingService, IInventorySettings inventorySettings, IMapper mapper)
        {
            _settingService = settingService ?? throw new ArgumentNullException(nameof(settingService));
            _inventorySettings = inventorySettings ?? throw new ArgumentNullException(nameof(inventorySettings));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        /// <summary>
        /// Add a setting. Requires authentication.
        /// </summary>
        /// <param name="setting">
        /// Setting request object. Include key and value. Key is unique in database.
        /// </param>
        [HttpPost]
        [Route("set-setting")]
        [Authorize]
        [RequirePermission(PermissionCodes.SettingManage)]
        [ProducesResponseType((int)HttpStatusCode.Created)]
        public async Task<IActionResult> SetSetting([FromBody] SettingRequest setting)
        {
            var result = await _settingService.SetSettingAsync(setting);
            if (setting?.Key?.StartsWith(SettingKeys.InventoryPrefix, StringComparison.OrdinalIgnoreCase) == true)
                _inventorySettings.InvalidateCache();
            var settingResponse = _mapper.Map<SettingResponse>(result);
            return Ok(new ApiOkResponse(settingResponse));
        }

        /// <summary>
        /// Get setting. Requires authentication.
        /// </summary>
        /// <param name="key">Setting key.</param>
        [HttpGet()]
        [Authorize]
        [RequirePermission(PermissionCodes.SettingRead)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetSetting(string key)
        {
            var result = await _settingService.GetSettingAsync(key);
            return Ok(new ApiOkResponse(result));
        }

        /// <summary>
        /// Get all settings grouped by category (Inventory, Company, Notifications). Uses defaults for missing keys.
        /// </summary>
        [HttpGet("grouped")]
        [Authorize]
        [RequirePermission(PermissionCodes.SettingRead)]
        [ProducesResponseType(typeof(GroupedSettingsResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetGrouped()
        {
            var all = await _settingService.GetAllAsync();
            var dict = all.ToDictionary(s => s.Key, s => s.Value, StringComparer.OrdinalIgnoreCase);
            var response = new GroupedSettingsResponse
            {
                Inventory = new InventorySettingsDto
                {
                    RoundingDecimals = ParseInt(dict, SettingKeys.RoundingDecimals, SettingKeys.RoundingDecimalsDefault),
                    PriceRoundingDecimals = ParseInt(dict, SettingKeys.PriceRoundingDecimals, SettingKeys.PriceRoundingDecimalsDefault),
                    AllowNegativeStock = ParseBool(dict, SettingKeys.AllowNegativeStock, SettingKeys.AllowNegativeStockDefault),
                    DefaultUnitOfMeasure = GetString(dict, SettingKeys.DefaultUnitOfMeasure, SettingKeys.DefaultUnitOfMeasureDefault)
                },
                Company = new CompanySettingsDto
                {
                    Name = GetString(dict, SettingKeys.CompanyName, SettingKeys.CompanyNameDefault),
                    TaxId = GetString(dict, SettingKeys.CompanyTaxId, SettingKeys.CompanyTaxIdDefault)
                },
                Notifications = new NotificationsSettingsDto
                {
                    AlertOnLowStock = ParseBool(dict, SettingKeys.NotificationsAlertOnLowStock, SettingKeys.NotificationsAlertOnLowStockDefault),
                    LowStockRecipients = GetString(dict, SettingKeys.NotificationsLowStockRecipients, SettingKeys.NotificationsLowStockRecipientsDefault)
                }
            };
            return Ok(new ApiOkResponse(response));
        }

        /// <summary>
        /// Update grouped settings. Only sent sections are updated. Invalidates inventory settings cache if any Inventory key is updated.
        /// </summary>
        [HttpPut("grouped")]
        [Authorize]
        [RequirePermission(PermissionCodes.SettingManage)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public async Task<IActionResult> PutGrouped([FromBody] UpdateGroupedSettingsRequest request)
        {
            if (request == null)
                return Ok(new ApiOkResponse(new { updated = 0 }));

            var inventoryUpdated = false;
            if (request.Inventory != null)
            {
                if (request.Inventory.RoundingDecimals.HasValue)
                    await _settingService.SetSettingAsync(new SettingRequest { Key = SettingKeys.RoundingDecimals, Value = request.Inventory.RoundingDecimals.Value.ToString(CultureInfo.InvariantCulture) });
                if (request.Inventory.PriceRoundingDecimals.HasValue)
                    await _settingService.SetSettingAsync(new SettingRequest { Key = SettingKeys.PriceRoundingDecimals, Value = request.Inventory.PriceRoundingDecimals.Value.ToString(CultureInfo.InvariantCulture) });
                if (request.Inventory.AllowNegativeStock.HasValue)
                    await _settingService.SetSettingAsync(new SettingRequest { Key = SettingKeys.AllowNegativeStock, Value = request.Inventory.AllowNegativeStock.Value.ToString(CultureInfo.InvariantCulture) });
                if (request.Inventory.DefaultUnitOfMeasure != null)
                    await _settingService.SetSettingAsync(new SettingRequest { Key = SettingKeys.DefaultUnitOfMeasure, Value = request.Inventory.DefaultUnitOfMeasure });
                inventoryUpdated = true;
            }
            if (request.Company != null)
            {
                if (request.Company.Name != null)
                    await _settingService.SetSettingAsync(new SettingRequest { Key = SettingKeys.CompanyName, Value = request.Company.Name });
                if (request.Company.TaxId != null)
                    await _settingService.SetSettingAsync(new SettingRequest { Key = SettingKeys.CompanyTaxId, Value = request.Company.TaxId });
            }
            if (request.Notifications != null)
            {
                if (request.Notifications.AlertOnLowStock.HasValue)
                    await _settingService.SetSettingAsync(new SettingRequest { Key = SettingKeys.NotificationsAlertOnLowStock, Value = request.Notifications.AlertOnLowStock.Value.ToString(CultureInfo.InvariantCulture) });
                if (request.Notifications.LowStockRecipients != null)
                    await _settingService.SetSettingAsync(new SettingRequest { Key = SettingKeys.NotificationsLowStockRecipients, Value = request.Notifications.LowStockRecipients });
            }
            if (inventoryUpdated)
                _inventorySettings.InvalidateCache();
            return Ok(new ApiOkResponse(new { updated = true }));
        }

        private static int ParseInt(IReadOnlyDictionary<string, string> dict, string key, int defaultValue)
        {
            if (!dict.TryGetValue(key, out var v) || string.IsNullOrWhiteSpace(v)) return defaultValue;
            return int.TryParse(v.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : defaultValue;
        }

        private static bool ParseBool(IReadOnlyDictionary<string, string> dict, string key, bool defaultValue)
        {
            if (!dict.TryGetValue(key, out var v) || string.IsNullOrWhiteSpace(v)) return defaultValue;
            var lower = v.Trim().ToLowerInvariant();
            if (lower == "1" || lower == "true" || lower == "yes") return true;
            if (lower == "0" || lower == "false" || lower == "no") return false;
            return defaultValue;
        }

        private static string GetString(IReadOnlyDictionary<string, string> dict, string key, string defaultValue)
        {
            return dict.TryGetValue(key, out var v) ? (v ?? defaultValue) : defaultValue;
        }
    }
}