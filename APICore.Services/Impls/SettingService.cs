using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using APICore.Common.DTO.Request;
using APICore.Data;
using APICore.Data.Entities;
using APICore.Data.UoW;
using APICore.Services.Exceptions;
using Microsoft.Extensions.Localization;

namespace APICore.Services.Impls
{
    public class SettingService : ISettingService
    {
        private readonly IUnitOfWork _uow;
        private readonly CoreDbContext _context;
        private readonly IStringLocalizer<IAccountService> _localizer;

        public SettingService(IUnitOfWork uow, CoreDbContext context, IStringLocalizer<IAccountService> localizer)
        {
            _uow = uow ?? throw new ArgumentNullException(nameof(uow));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public async Task<string> GetSettingAsync(string settingKey)
        {
            if (string.IsNullOrEmpty(settingKey))
            {
                throw new ArgumentNullException(nameof(settingKey));
            }
            var orgId = _context.CurrentOrganizationId;
            var setting = orgId > 0
                ? await _uow.SettingRepository.FirstOrDefaultAsync(s => s.Key == settingKey && s.OrganizationId == orgId)
                    ?? await _uow.SettingRepository.FirstOrDefaultAsync(s => s.Key == settingKey && s.OrganizationId == null)
                : await _uow.SettingRepository.FirstOrDefaultAsync(s => s.Key == settingKey && s.OrganizationId == null);
            if (setting == null)
            {
                throw new SettingNotFoundException(_localizer);
            }

            return setting.Value;
        }

        public async Task<string> GetSettingOrDefaultAsync(string key, string defaultValue)
        {
            if (string.IsNullOrEmpty(key))
                return defaultValue;
            var orgId = _context.CurrentOrganizationId;
            var setting = orgId > 0
                ? await _uow.SettingRepository.FirstOrDefaultAsync(s => s.Key == key && s.OrganizationId == orgId)
                    ?? await _uow.SettingRepository.FirstOrDefaultAsync(s => s.Key == key && s.OrganizationId == null)
                : await _uow.SettingRepository.FirstOrDefaultAsync(s => s.Key == key && s.OrganizationId == null);
            return setting?.Value ?? defaultValue;
        }

        public async Task<IReadOnlyList<Setting>> GetAllAsync()
        {
            var list = await _uow.SettingRepository.GetAllAsync();
            return list?.ToList() ?? new List<Setting>();
        }

        public async Task<Setting> SetSettingAsync(SettingRequest settingRequest)
        {
            if (settingRequest == null)
            {
                throw new ArgumentNullException(nameof(settingRequest));
            }

            var orgIdRaw = _context.CurrentOrganizationId;
            int? orgId = orgIdRaw > 0 ? orgIdRaw : (int?)null;
            var result = await _uow.SettingRepository
                .FirstOrDefaultAsync(s => s.Key == settingRequest.Key && s.OrganizationId == orgId);

            if (result != null)
            {
                result.Value = settingRequest.Value;
                _uow.SettingRepository.Update(result);
            }
            else
            {
                result = new Setting { OrganizationId = orgId, Key = settingRequest.Key, Value = settingRequest.Value };
                await _uow.SettingRepository.AddAsync(result);
            }

            await _uow.CommitAsync();

            return result;
        }
    }
}
