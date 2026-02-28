using Microsoft.Extensions.Localization;

namespace APICore.Services.Exceptions
{
    public class RoleInUseCannotDeleteBadRequestException : BaseBadRequestException
    {
        public RoleInUseCannotDeleteBadRequestException(IStringLocalizer<object> localizer) : base()
        {
            CustomCode = 400021;
            CustomMessage = localizer.GetString(CustomCode.ToString());
        }
    }
}
