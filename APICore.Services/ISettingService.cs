using APICore.Common.DTO.Request;
using APICore.Data.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace APICore.Services
{
    public interface ISettingService
    {
        Task<Setting> SetSettingAsync(SettingRequest settingRequest);

        Task<string> GetSettingAsync(string settingKey);

        /// <summary>
        /// Returns the setting value if the key exists; otherwise returns <paramref name="defaultValue"/>.
        /// Does not throw when the key is missing.
        /// </summary>
        Task<string> GetSettingOrDefaultAsync(string key, string defaultValue);

        /// <summary>
        /// Returns all settings as key-value pairs for the grouped configuration endpoint.
        /// </summary>
        Task<IReadOnlyList<Setting>> GetAllAsync();
    }
}