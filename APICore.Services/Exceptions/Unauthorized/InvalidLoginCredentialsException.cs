using APICore.Services;
using Microsoft.Extensions.Localization;

namespace APICore.Services.Exceptions
{
    /// <summary>
    /// Credenciales incorrectas en login (no usar el mensaje genérico 401001 de permisos).
    /// </summary>
    public class InvalidLoginCredentialsException : BaseUnauthorizedException
    {
        public InvalidLoginCredentialsException(IStringLocalizer<IAccountService> localizer) : base()
        {
            CustomCode = 401003;
            CustomMessage = localizer.GetString(CustomCode.ToString());
        }
    }
}
