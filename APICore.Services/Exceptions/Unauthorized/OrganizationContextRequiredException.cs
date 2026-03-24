using Microsoft.Extensions.Localization;

namespace APICore.Services.Exceptions
{
    /// <summary>
    /// 401: el usuario autenticado no tiene organización asociada y el recurso exige contexto de organización.
    /// Distinto de 401001 (acceso no autorizado genérico / sin permiso).
    /// </summary>
    public class OrganizationContextRequiredException : BaseUnauthorizedException
    {
        public OrganizationContextRequiredException(IStringLocalizer<object> localizer) : base()
        {
            CustomCode = 401002;
            CustomMessage = localizer.GetString(CustomCode.ToString());
        }
    }
}
