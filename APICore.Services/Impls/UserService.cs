using APICore.Common.DTO.Request;
using APICore.Data.Entities;
using APICore.Services;
using APICore.Data.UoW;
using APICore.Services.Exceptions;
using APICore.Services.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace APICore.Services.Impls
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _uow;
        private readonly IStringLocalizer<IUserService> _localizer;
        private readonly ICurrentUserContextAccessor _currentUserContextAccessor;

        public UserService(IUnitOfWork uow, IStringLocalizer<IUserService> localizer, ICurrentUserContextAccessor currentUserContextAccessor)
        {
            _uow = uow;
            _localizer = localizer;
            _currentUserContextAccessor = currentUserContextAccessor ?? throw new ArgumentNullException(nameof(currentUserContextAccessor));
        }

        public async Task<User> CreateUser(CreateUserRequest user)
        {
            if (user.Email == "")
            {
                throw new EmptyEmailBadRequestException(_localizer);
            }
            var emailExists = await _uow.UserRepository.FindAllAsync(u => u.Email == user.Email);
            if (emailExists != null && emailExists.Count > 0)
            {
                throw new EmailInUseBadRequestException(_localizer);
            }
            if (string.IsNullOrWhiteSpace(user.Password) || user.Password.Length < 6 || CheckStringWithoutSpecialChars(user.Password) || !CheckStringWithUppercaseLetters(user.Password))
            {
                throw new PasswordRequirementsBadRequestException(_localizer);
            }

            var currentContext = _currentUserContextAccessor.GetCurrent();
            var organizationIdToAssign = currentContext?.OrganizationId;
            if (!organizationIdToAssign.HasValue)
            {
                throw new OrganizationRequiredToCreateUserBadRequestException(_localizer);
            }

            if (user.LocationId.HasValue)
            {
                var location = await _uow.LocationRepository.FindBy(l => l.Id == user.LocationId.Value).FirstOrDefaultAsync();
                if (location == null || location.OrganizationId != organizationIdToAssign.Value)
                {
                    throw new LocationNotInOrganizationBadRequestException(_localizer);
                }
            }

            var hashed_password = GetSha256Hash(user.Password);

            var new_user = new User
            {
                Email = user.Email,
                Password = hashed_password,
                FullName = user.FullName,
                Phone = user.Phone,
                BirthDate = user.BirthDate,
                Status = Data.Entities.Enums.StatusEnum.ACTIVE,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
                LocationId = user.LocationId,
                OrganizationId = organizationIdToAssign.Value,
                RoleId = user.RoleId,
            };

            await _uow.UserRepository.AddAsync(new_user);
            await _uow.CommitAsync();

            return new_user;
        }

        public async Task DeleteUser(int id)
        {
            var user = await _uow.UserRepository.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                throw new UserNotFoundException(_localizer);
            }
            _uow.UserRepository.Delete(user);
            await _uow.CommitAsync();
        }

        public async Task<PaginatedList<User>> GetAllUsers(int? page, int? perPage, string sortOrder = null)
        {
            var users = _uow.UserRepository.GetAll()
                .Include(u => u.Location)
                .Include(u => u.Organization);
            var pageIndex = page ?? 1;
            var perPageIndex = perPage ?? 10;
            return await PaginatedList<User>.CreateAsync(users, pageIndex, perPageIndex);
        }

        public async Task<User> GetUser(int id)
        {
            var user = await _uow.UserRepository.FindBy(u => u.Id == id)
                .Include(u => u.Location)
                .Include(u => u.Organization)
                .FirstOrDefaultAsync();
            if (user == null)
            {
                throw new UserNotFoundException(_localizer);
            }
            return user;
        }

        public async Task UpdateUser(int id, UpdateUserRequest user)
        {
            var old_user = await _uow.UserRepository.FirstOrDefaultAsync(u => u.Id == id);
            if (old_user == null)
            {
                throw new UserNotFoundException(_localizer);
            }

            if (user.Password != null)
            {
                if (string.IsNullOrWhiteSpace(user.Password) || user.Password.Length < 6 || CheckStringWithoutSpecialChars(user.Password) || !CheckStringWithUppercaseLetters(user.Password))
                {
                    throw new PasswordRequirementsBadRequestException(_localizer);
                }
                var old_password_user = GetSha256Hash(user.OldPassword);
                if (old_password_user != old_user.Password)
                {
                    throw new PasswordsDoesntMatchBadRequestException(_localizer);
                }
            }

            var new_password = user.Password != null ? GetSha256Hash(user.Password) : old_user.Password;

            var new_user = new User
            {
                Id = old_user.Id,
                CreatedAt = old_user.CreatedAt,
                ModifiedAt = DateTime.UtcNow,
                FullName = user.FullName ?? old_user.FullName,
                Email = user.Email ?? old_user.Email,
                Password = new_password,
                Phone = user.Phone ?? old_user.Phone,
                BirthDate = old_user.BirthDate,
                Status = old_user.Status,
                LocationId = user.LocationId ?? old_user.LocationId,
                OrganizationId = user.OrganizationId ?? old_user.OrganizationId,
                RoleId = user.RoleId ?? old_user.RoleId,
            };

            await _uow.UserRepository.UpdateAsync(new_user, old_user.Id);
            await _uow.CommitAsync();
        }

        private bool CheckStringWithoutSpecialChars(string word)
        {
            var regexItem = new Regex("^[a-zA-Z0-9 ]*$");
            return regexItem.IsMatch(word);
        }

        private bool CheckStringWithUppercaseLetters(string word)
        {
            var regexItem = new Regex("[A-Z]");
            return regexItem.IsMatch(word);
        }

        private string GetSha256Hash(string input)
        {
            using (var hashAlgorithm = SHA256.Create())
            {
                var byteValue = Encoding.UTF8.GetBytes(input);
                var byteHash = hashAlgorithm.ComputeHash(byteValue);
                return Convert.ToBase64String(byteHash);
            }
        }
    }
}
