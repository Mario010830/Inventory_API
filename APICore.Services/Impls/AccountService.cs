using APICore.Common.Constants;
using APICore.Common.DTO.Request;
using APICore.Data;
using APICore.Data.Entities;
using APICore.Data.Entities.Enums;
using APICore.Data.UoW;
using APICore.Services.Exceptions;
using Google.Apis.Auth;
using DeviceDetectorNET;
using DeviceDetectorNET.Cache;
using DeviceDetectorNET.Parser;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.IdentityModel.Tokens;
using rlcx.suid;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Wangkanai.Detection.Services;

namespace APICore.Services.Impls
{
    public class AccountService : IAccountService
    {
        private readonly IConfiguration _configuration;

        private readonly IUnitOfWork _uow;

        private readonly CoreDbContext _context;
        private readonly IStringLocalizer<IAccountService> _localizer;
        private readonly IDetectionService _detectionService;

        public AccountService(IConfiguration configuration, IUnitOfWork uow,
            CoreDbContext context,
            IStringLocalizer<IAccountService> localizer,
            IDetectionService detectionService)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _uow = uow ?? throw new ArgumentNullException(nameof(uow));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
            _detectionService = detectionService ?? throw new ArgumentNullException(nameof(detectionService));
        }

       
        private Task<User> GetUserByEmailIgnoringFiltersAsync(string email) =>
            _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email == email);

       
        private Task<User> GetUserByIdIgnoringFiltersAsync(int id) =>
            _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == id);

       
        private Task<User> GetUserByGoogleIdIgnoringFiltersAsync(string googleId) =>
            _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.GoogleId == googleId);

        private Task<Role> GetRoleIgnoringFiltersAsync(string role) =>
            _context.Roles.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Name == role);

        private Task<User> GetUserByEmailForLoginAsync(string email) =>
            _context.Users.IgnoreQueryFilters().Include(u => u.Location).Include(u => u.Organization).FirstOrDefaultAsync(u => u.Email == email);

        public async Task<(User user, string accessToken, string refreshToken)> LoginAsync(LoginRequest loginRequest)
        {
            var hashedPass = GetSha256Hash(loginRequest.Password);

            var user = await GetUserByEmailForLoginAsync(loginRequest.Email);

            if (user == null)
            {
                throw new UserNotFoundException(_localizer);
            }

            if (user.Password != hashedPass)
            {
                throw new UnauthorizedException(_localizer);
            }
            if (user.Status != StatusEnum.ACTIVE)
            {
                throw new AccountInactiveForbiddenException(_localizer);
            }

            var dd = GetDeviceDetectorConfigured();

            var clientInfo = dd.GetClient();
            var osrInfo = dd.GetOs();
            var device1 = dd.GetDeviceName();
            var brand = dd.GetBrandName();
            var model = dd.GetModel();

            var claims = GetClaims(user);
            var token = GetToken(claims);
            var refreshToken = GetRefreshToken();
            var t = new UserToken();
            t.AccessToken = token;
            t.AccessTokenExpiresDateTime = DateTime.UtcNow.AddHours(int.Parse(_configuration.GetSection("BearerTokens")["AccessTokenExpirationHours"]));
            t.RefreshToken = refreshToken;
            t.RefreshTokenExpiresDateTime = DateTime.UtcNow.AddHours(int.Parse(_configuration.GetSection("BearerTokens")["RefreshTokenExpirationHours"]));
            t.UserId = user.Id;

            t.DeviceModel = model;
            t.DeviceBrand = brand;

            t.OS = osrInfo.Match?.Name;
            t.OSPlatform = osrInfo.Match?.Platform;
            t.OSVersion = osrInfo.Match?.Version;

            t.ClientName = clientInfo.Match?.Name;
            t.ClientType = clientInfo.Match?.Type;
            t.ClientVersion = clientInfo.Match?.Version;

            await _uow.UserTokenRepository.AddAsync(t);
            await _uow.CommitAsync();

            return (user, token, refreshToken);
        }

        public async Task<(User user, string accessToken, string refreshToken)> LoginWithGoogleAsync(GoogleLoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.IdToken))
                throw new InvalidGoogleTokenBadRequestException(_localizer);

            var clientId = _configuration["Google:ClientId"];
            if (string.IsNullOrWhiteSpace(clientId))
                throw new InvalidOperationException("Google:ClientId is not configured.");

            GoogleJsonWebSignature.Payload payload;
            try
            {
                payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { clientId }
                });
            }
            catch (InvalidJwtException)
            {
                throw new InvalidGoogleTokenBadRequestException(_localizer);
            }

            var googleId = payload.Subject;
            var email = payload.Email ?? payload.Subject;
            var fullName = payload.Name ?? email;

            var user = await GetUserByGoogleIdIgnoringFiltersAsync(googleId);
            if (user == null)
            {
                user = await GetUserByEmailIgnoringFiltersAsync(email);
                if (user != null)
                {
                    if (user.GoogleId == null)
                    {
                        user.GoogleId = googleId;
                        user.ModifiedAt = DateTime.UtcNow;
                        await _uow.UserRepository.UpdateAsync(user, user.Id);
                        await _uow.CommitAsync();
                    }
                }
                else
                {
                    var placeholderPassword = GetSha256Hash(Guid.NewGuid().ToString());
                    user = new User
                    {
                        Email = email,
                        FullName = fullName,
                        GoogleId = googleId,
                        Password = placeholderPassword,
                        BirthDate = new DateTime(2000, 1, 1),
                        Phone = null,
                        Status = StatusEnum.ACTIVE,
                        CreatedAt = DateTime.UtcNow,
                        ModifiedAt = DateTime.UtcNow,
                    };
                    await _uow.UserRepository.AddAsync(user);
                    await _uow.CommitAsync();
                }
            }

            if (user.Status != StatusEnum.ACTIVE)
                throw new AccountInactiveForbiddenException(_localizer);

            var dd = GetDeviceDetectorConfigured();
            var clientInfo = dd.GetClient();
            var osrInfo = dd.GetOs();
            var brand = dd.GetBrandName();
            var model = dd.GetModel();

            var claims = GetClaims(user);
            var token = GetToken(claims);
            var refreshToken = GetRefreshToken();
            var t = new UserToken
            {
                AccessToken = token,
                AccessTokenExpiresDateTime = DateTime.UtcNow.AddHours(int.Parse(_configuration.GetSection("BearerTokens")["AccessTokenExpirationHours"])),
                RefreshToken = refreshToken,
                RefreshTokenExpiresDateTime = DateTime.UtcNow.AddHours(int.Parse(_configuration.GetSection("BearerTokens")["RefreshTokenExpirationHours"])),
                UserId = user.Id,
                DeviceModel = model,
                DeviceBrand = brand,
                OS = osrInfo.Match?.Name,
                OSPlatform = osrInfo.Match?.Platform,
                OSVersion = osrInfo.Match?.Version,
                ClientName = clientInfo.Match?.Name,
                ClientType = clientInfo.Match?.Type,
                ClientVersion = clientInfo.Match?.Version,
            };
            await _uow.UserTokenRepository.AddAsync(t);
            await _uow.CommitAsync();

            return (user, token, refreshToken);
        }

        private string GetRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        private string GetToken(IEnumerable<Claim> claims)
        {
            var issuer = _configuration.GetSection("BearerTokens")["Issuer"];
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration.GetSection("BearerTokens")["Key"]));

            var jwt = new JwtSecurityToken(issuer: issuer,
                audience: _configuration.GetSection("BearerTokens")["Audience"],
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddHours(int.Parse(_configuration.GetSection("BearerTokens")["AccessTokenExpirationHours"])),
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(jwt);
        }

        public async Task GlobalLogoutAsync(int userId)
        {
            if (userId > 0)
            {
                var user = await GetUserByIdIgnoringFiltersAsync(userId);

                // Check for wrong or not existant user
                if (user == null)
                {
                    throw new UserNotFoundException(_localizer);
                }

                // Check for inactive user
                if (user.Status == StatusEnum.INACTIVE)
                {
                    throw new AccountInactiveForbiddenException(_localizer);
                }

                var tokens = await _uow.UserTokenRepository.FindByAsync(t => t.UserId == userId);

                // Only do a commit when you actually delete something
                if (tokens != null)
                {
                    if (tokens.Count > 0)
                    {
                        foreach (var item in tokens)
                        {
                            _uow.UserTokenRepository.Delete(item);
                        }
                        await _uow.CommitAsync();
                    }
                }
            }
            else
            {
                throw new UserNotFoundException(_localizer);
            }
        }

        public async Task LogoutAsync(string accessToken, int userId)
        {
            // Null or empty parameters check
            if (string.IsNullOrEmpty(accessToken))
            {
                throw new ArgumentNullException(nameof(accessToken));
            }

            var token = accessToken.Split("Bearer")[1].Trim();

            if (userId > 0 && !string.IsNullOrEmpty(token))
            {
                var user = await GetUserByIdIgnoringFiltersAsync(userId);

                // Check for wrong or not existant user
                if (user == null)
                {
                    throw new UserNotFoundException(_localizer);
                }

                // Check for inactive user
                if (user.Status == StatusEnum.INACTIVE)
                {
                    throw new AccountInactiveForbiddenException(_localizer);
                }

                var tokens = await _uow.UserTokenRepository.FindByAsync(t => t.UserId == userId && t.AccessToken == accessToken);

                // Only do a commit when you actually delete something
                if (tokens != null)
                {
                    if (tokens.Count > 0)
                    {
                        foreach (var item in tokens)
                        {
                            _uow.UserTokenRepository.Delete(item);
                        }
                        await _uow.CommitAsync();
                    }
                }
            }
            else
            {
                throw new UserNotFoundException(_localizer);
            }
        }

        public async Task SignUpAsync(SignUpRequest suRequest)
        {
            if (suRequest.Email == "")
            {
                throw new EmptyEmailBadRequestException(_localizer);
            }
            var emailExists = await GetUserByEmailIgnoringFiltersAsync(suRequest.Email);
            if (emailExists != null)
            {
                throw new EmailInUseBadRequestException(_localizer);
            }

            if (string.IsNullOrWhiteSpace(suRequest.Password) ||
                suRequest.Password.Length < 6
                || CheckStringWithoutSpecialChars(suRequest.Password)
                || !CheckStringWithUppercaseLetters(suRequest.Password))
            {
                throw new PasswordRequirementsBadRequestException(_localizer);
            }

            if (suRequest.Password != suRequest.ConfirmationPassword)
            {
                throw new PasswordsDoesntMatchBadRequestException(_localizer);
            }

            var organization = await _uow.OrganizationRepository.FindBy(o => o.Id == suRequest.OrganizationId).FirstOrDefaultAsync();
            if (organization == null)
            {
                throw new OrganizationNotFoundException(_localizer);
            }

            var adminRole = await _uow.RoleRepository.FindBy(r => r.Name == RoleNames.Admin).FirstOrDefaultAsync();
            if (adminRole == null)
            {
                throw new RoleNotFoundException(_localizer);
            }

            var passwordHash = GetSha256Hash(suRequest.Password);
            var user = new User
            {
                Email = suRequest.Email,
                FullName = suRequest.FullName,
                BirthDate = suRequest.Birthday,
                Phone = suRequest.Phone,
                Password = passwordHash,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
                Status = StatusEnum.ACTIVE,
                OrganizationId = suRequest.OrganizationId,
                LocationId = null,
                RoleId = adminRole.Id
            };

            await _uow.UserRepository.AddAsync(user);

            await _uow.CommitAsync();
        }

        public async Task SignUpWithOrganizationAsync(RegisterWithOrganizationRequest request)
        {
         
            var codeExists = await _uow.OrganizationRepository.FindBy(o => o.Code == request.OrganizationCode.Trim()).AnyAsync();
            if (codeExists)
                throw new OrganizationCodeInUseBadRequestException(_localizer);

            
            if (string.IsNullOrWhiteSpace(request.Email))
                throw new EmptyEmailBadRequestException(_localizer);

            var emailExists = await GetUserByEmailIgnoringFiltersAsync(request.Email);
            if (emailExists != null)
                throw new EmailInUseBadRequestException(_localizer);

            if (string.IsNullOrWhiteSpace(request.Password) ||
                request.Password.Length < 6 ||
                CheckStringWithoutSpecialChars(request.Password) ||
                !CheckStringWithUppercaseLetters(request.Password))
                throw new PasswordRequirementsBadRequestException(_localizer);

            if (request.Password != request.ConfirmationPassword)
                throw new PasswordsDoesntMatchBadRequestException(_localizer);

            var adminRole = await GetRoleIgnoringFiltersAsync(RoleNames.Admin);
            var rolesRepository = await _uow.RoleRepository.GetAllAsync();

            var rolesText = string.Join(", ", rolesRepository.Select(r => r.Name));

          
            var organization = new Organization
            {
                Name = request.OrganizationName,
                Code = request.OrganizationCode.Trim(),
                Description = request.OrganizationDescription?.Trim(),
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
            };
            await _uow.OrganizationRepository.AddAsync(organization);

            var passwordHash = GetSha256Hash(request.Password);
            var user = new User
            {
                Email = request.Email,
                FullName = request.FullName,
                BirthDate = request.Birthday,
                Phone = request.Phone,
                Password = passwordHash,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
                Status = StatusEnum.ACTIVE,
                Organization = organization,
                LocationId = null,
                RoleId = adminRole.Id
            };
            await _uow.UserRepository.AddAsync(user);

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

        private static string NormalizeAccessToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return token;
            var t = token.Trim();
            if (t.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                t = t.Substring(7).Trim();
            return t;
        }

        public Task<ClaimsPrincipal> GetPrincipalFromExpiredTokenAsync(string token)
        {
            token = NormalizeAccessToken(token);
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false, //you might want to validate the audience and issuer depending on your use case
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration.GetSection("BearerTokens")["Key"])),
                ValidateLifetime = false //here we are saying that we don't care about the token's expiration date
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            SecurityToken securityToken;
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out securityToken);
            var jwtSecurityToken = securityToken as JwtSecurityToken;
            if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                throw new InvalidTokenBadRequestException(_localizer);

            return Task.FromResult(principal);
        }

        public async Task GetRefreshTokenAsync(RefreshTokenRequest refreshToken, int userId)
        {
            var accessToken = NormalizeAccessToken(refreshToken.Token);
            var refToken = await _uow.UserTokenRepository
                .FirstOrDefaultAsync(u => u.UserId == userId && u.AccessToken == accessToken);
            if (refToken == null)
            {
                throw new RefreshTokenNotFoundException(_localizer);
            }
            if (refToken.RefreshToken != refreshToken.RefreshToken)
            {
                throw new InvalidRefreshTokenBadRequestException(_localizer);
            }
        }

        public async Task<(string accessToken, string refreshToken)> GenerateNewTokensAsync(string token, string refreshToken)
        {
            token = NormalizeAccessToken(token);
            var oldToken = await _context.Set<UserToken>()
                .IgnoreQueryFilters()
                .Where(u => u.AccessToken == token && u.RefreshToken == refreshToken)
                .Include(u => u.User)
                .FirstOrDefaultAsync();

            if (oldToken == null)
            {
                throw new UnauthorizedException(_localizer);
            }

            var claims = GetClaims(oldToken.User);

            var newToken = GetToken(claims);
            var newRefreshToken = GetRefreshToken();

            oldToken.AccessToken = newToken;
            oldToken.AccessTokenExpiresDateTime = DateTime.UtcNow.AddHours(int.Parse(_configuration.GetSection("BearerTokens")["AccessTokenExpirationHours"]));
            oldToken.RefreshToken = newRefreshToken;
            oldToken.RefreshTokenExpiresDateTime = DateTime.UtcNow.AddHours(int.Parse(_configuration.GetSection("BearerTokens")["RefreshTokenExpirationHours"]));

            _uow.UserTokenRepository.Update(oldToken);
            await _uow.CommitAsync();
            return (newToken, newRefreshToken);
        }

        private List<Claim> GetClaims(User user)
        {
            var issuer = _configuration.GetSection("BearerTokens")["Issuer"];
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, user.Email, ClaimValueTypes.Email, issuer),
                new Claim(ClaimTypes.AuthenticationMethod, "bearer", ClaimValueTypes.String, issuer),
                new Claim(ClaimTypes.NameIdentifier, user.FullName, ClaimValueTypes.String, issuer),
                new Claim(ClaimTypes.DateOfBirth, user.BirthDate.ToString(), ClaimValueTypes.Date, issuer),
                new Claim(ClaimTypes.UserData, user.Id.ToString(), ClaimValueTypes.String, issuer)
            };
            return claims;
        }

        public async Task ChangePasswordAsync(ChangePasswordRequest changePassword, int userId)
        {
            var user = await GetUserByIdIgnoringFiltersAsync(userId);
            if (user == null)
                throw new UserNotFoundException(_localizer);
            var passwordHash = GetSha256Hash(changePassword.OldPassword);

            if (passwordHash != user.Password)
            {
                throw new OldPasswordIncorrectBadRequestException(_localizer);
            }

            if (changePassword.NewPassword != changePassword.ConfirmPassword)
            {
                throw new PasswordsDoesntMatchBadRequestException(_localizer);
            }

            if (string.IsNullOrWhiteSpace(changePassword.NewPassword) ||
                changePassword.NewPassword.Length < 6
                || CheckStringWithoutSpecialChars(changePassword.NewPassword)
                || !CheckStringWithUppercaseLetters(changePassword.NewPassword))
            {
                throw new PasswordRequirementsBadRequestException(_localizer);
            }

            var newPasswordHash = GetSha256Hash(changePassword.NewPassword);

            user.Password = newPasswordHash;
            user.ModifiedAt = DateTime.UtcNow;

            _uow.UserRepository.Update(user);

            await _uow.CommitAsync();
        }

        private DeviceDetector GetDeviceDetectorConfigured()
        {
            var ua = _detectionService.UserAgent;

            DeviceDetector.SetVersionTruncation(VersionTruncation.VERSION_TRUNCATION_NONE);

            var dd = new DeviceDetector(ua.ToString());

            // OPTIONAL: Set caching method By default static cache is used, which works best within one
            // php process (memory array caching) To cache across requests use caching in files or
            // memcache add using DeviceDetectorNET.Cache;
            dd.SetCache(new DictionaryCache());

            // OPTIONAL: If called, GetBot() will only return true if a bot was detected (speeds up
            // detection a bit)
            dd.DiscardBotInformation();

            // OPTIONAL: If called, bot detection will completely be skipped (bots will be detected as
            // regular devices then)
            dd.SkipBotDetection();
            dd.Parse();
            return dd;
        }

        public async Task<User> UpdateProfileAsync(UpdateProfileRequest updateProfile, int userId)
        {
            var user = await GetUserByIdIgnoringFiltersAsync(userId);

            if (user == null)
            {
                throw new UserNotFoundException(_localizer);
            }

            user.FullName = updateProfile.FullName;
            user.BirthDate = updateProfile.Birthday;
            user.Phone = updateProfile.Phone;
            user.ModifiedAt = DateTime.UtcNow;
          

            _uow.UserRepository.Update(user);

            await _uow.CommitAsync();

            return user;
        }

        public Task<bool> ValidateTokenAsync(string token)
        {
            var isValid = true;

            var validator = new JwtSecurityTokenHandler();

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidAudience = _configuration.GetSection("BearerTokens")["Audience"],
                ValidateAudience = true,
                ValidIssuer = _configuration.GetSection("BearerTokens")["Issuer"],
                ValidateIssuer = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration.GetSection("BearerTokens")["Key"])),
                ValidateLifetime = true
            };

            if (validator.CanReadToken(token))
            {
                try
                {
                    SecurityToken securityToken;
                    var principal = validator.ValidateToken(token, tokenValidationParameters, out securityToken);
                }
                catch (Exception)
                {
                    isValid = false;
                }
            }
            else
            {
                isValid = false;
            }

            return Task.FromResult(isValid);
        }

        public async Task<User> GetUserAsync(int userId)
        {
            var user = await GetUserByIdIgnoringFiltersAsync(userId);
            if (user == null)
            {
                throw new UserNotFoundException(_localizer);
            }

            return user;
        }

        public async Task ChangeAccountStatusAsync(ChangeAccountStatusRequest changeAccountStatus, int userId)
        {
            // Check if the Master exist
            var master = await GetUserByIdIgnoringFiltersAsync(userId);

            if (master == null)
            {
                throw new UserNotFoundException(_localizer);
            }

            // Find the Inactive User
            var user = await GetUserByEmailIgnoringFiltersAsync(changeAccountStatus.Email);

            if (user == null)
            {
                throw new UserNotFoundException(_localizer);
            }

            // You can't reActivate yourself
            if (user.Id == userId && user.Status == StatusEnum.INACTIVE)
            {
                throw new AccountDeactivatedForbiddenException(_localizer);
            }

            if (changeAccountStatus.Active == false)
            {
                user.Status = StatusEnum.INACTIVE;
            }
            else
            {
                user.Status = StatusEnum.ACTIVE;
            }

            user.ModifiedAt = DateTime.UtcNow;

            _uow.UserRepository.Update(user);

            await _uow.CommitAsync();
        }


        public async Task<string> ForgotPasswordAsync(string email)
        {
            var user = await GetUserByEmailIgnoringFiltersAsync(email);
            if (user == null)
            {
                throw new UserNotFoundException(_localizer);
            }

            string newPass = Guid.NewGuid().ToString();

            var newPasswordHash = GetSha256Hash(newPass);

            user.Password = newPasswordHash;
            user.ModifiedAt = DateTime.UtcNow;
            await _uow.UserRepository.UpdateAsync(user, user.Id);
            await _uow.CommitAsync();
            return newPass;
        }
    }
}
