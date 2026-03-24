using Microsoft.Extensions.Localization;

namespace APICore.Services.Exceptions
{
    public class MaxProductImagesBadRequestException : BaseBadRequestException
    {
        public MaxProductImagesBadRequestException(IStringLocalizer<object> localizer) : base()
        {
            CustomCode = 400462;
            CustomMessage = localizer.GetString(CustomCode.ToString());
        }
    }
}
