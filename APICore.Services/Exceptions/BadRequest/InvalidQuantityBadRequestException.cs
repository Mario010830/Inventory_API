using Microsoft.Extensions.Localization;

namespace APICore.Services.Exceptions
{
    public class InvalidQuantityBadRequestException : BaseBadRequestException
    {
        public InvalidQuantityBadRequestException(IStringLocalizer<object> localizer) : base()
        {
            CustomCode = 400007;
            CustomMessage = localizer.GetString(CustomCode.ToString());
        }
    }
}
