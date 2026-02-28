using Microsoft.Extensions.Localization;

namespace APICore.Services.Exceptions
{
    public class OrganizationRequiredToCreateUserBadRequestException : BaseBadRequestException
    {
        public OrganizationRequiredToCreateUserBadRequestException(IStringLocalizer<object> localizer) : base()
        {
            CustomCode = 400029;
            CustomMessage = localizer.GetString(CustomCode.ToString());
        }
    }
}
