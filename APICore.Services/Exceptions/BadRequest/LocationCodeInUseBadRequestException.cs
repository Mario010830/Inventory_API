using Microsoft.Extensions.Localization;

namespace APICore.Services.Exceptions
{
    public class LocationCodeInUseBadRequestException : BaseBadRequestException
    {
        public LocationCodeInUseBadRequestException(IStringLocalizer<object> localizer) : base()
        {
            CustomCode = 400023;
            CustomMessage = localizer.GetString(CustomCode.ToString());
        }
    }
}
