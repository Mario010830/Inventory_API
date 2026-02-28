using Microsoft.Extensions.Localization;

namespace APICore.Services.Exceptions
{
    public class InvalidGoogleTokenBadRequestException : BaseBadRequestException
    {
        public InvalidGoogleTokenBadRequestException(IStringLocalizer<object> localizer) : base()
        {
            CustomCode = 400012;
            CustomMessage = localizer.GetString(CustomCode.ToString());
        }
    }
}
