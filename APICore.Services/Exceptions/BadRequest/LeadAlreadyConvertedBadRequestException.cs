using Microsoft.Extensions.Localization;

namespace APICore.Services.Exceptions
{
    public class LeadAlreadyConvertedBadRequestException : BaseBadRequestException
    {
        public LeadAlreadyConvertedBadRequestException(IStringLocalizer<object> localizer) : base()
        {
            CustomCode = 400032;
            CustomMessage = localizer.GetString(CustomCode.ToString());
        }
    }
}
