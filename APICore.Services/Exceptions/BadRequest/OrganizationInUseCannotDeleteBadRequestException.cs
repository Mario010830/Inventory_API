using Microsoft.Extensions.Localization;

namespace APICore.Services.Exceptions
{
    public class OrganizationInUseCannotDeleteBadRequestException : BaseBadRequestException
    {
        public OrganizationInUseCannotDeleteBadRequestException(IStringLocalizer<object> localizer) : base()
        {
            CustomCode = 400028;
            CustomMessage = localizer.GetString(CustomCode.ToString());
        }
    }
}
