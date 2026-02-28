using Microsoft.Extensions.Localization;

namespace APICore.Services.Exceptions
{
    public class LocationInUseCannotDeleteBadRequestException : BaseBadRequestException
    {
        public LocationInUseCannotDeleteBadRequestException(IStringLocalizer<object> localizer) : base()
        {
            CustomCode = 400024;
            CustomMessage = localizer.GetString(CustomCode.ToString());
        }
    }
}
