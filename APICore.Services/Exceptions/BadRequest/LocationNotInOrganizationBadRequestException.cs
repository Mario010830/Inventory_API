using Microsoft.Extensions.Localization;

namespace APICore.Services.Exceptions
{
    public class LocationNotInOrganizationBadRequestException : BaseBadRequestException
    {
        public LocationNotInOrganizationBadRequestException(IStringLocalizer<object> localizer) : base()
        {
            CustomCode = 400030;
            CustomMessage = localizer.GetString(CustomCode.ToString());
        }
    }
}
