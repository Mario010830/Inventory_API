using Microsoft.Extensions.Localization;

namespace APICore.Services.Exceptions
{
    public class RoleNameInUseBadRequestException : BaseBadRequestException
    {
        public RoleNameInUseBadRequestException(IStringLocalizer<object> localizer) : base()
        {
            CustomCode = 400022;
            CustomMessage = localizer.GetString(CustomCode.ToString());
        }
    }
}
