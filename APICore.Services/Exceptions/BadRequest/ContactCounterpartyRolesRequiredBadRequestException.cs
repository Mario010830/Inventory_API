using Microsoft.Extensions.Localization;

namespace APICore.Services.Exceptions
{
    public class ContactCounterpartyRolesRequiredBadRequestException : BaseBadRequestException
    {
        public ContactCounterpartyRolesRequiredBadRequestException(IStringLocalizer<object> localizer) : base()
        {
            CustomCode = 400510;
            CustomMessage = localizer.GetString(CustomCode.ToString());
        }
    }
}
