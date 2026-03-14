using Microsoft.Extensions.Localization;

namespace APICore.Services.Exceptions
{
    public class SaleReturnQuantityExceedsBadRequestException : BaseBadRequestException
    {
        public SaleReturnQuantityExceedsBadRequestException(IStringLocalizer<object> localizer) : base()
        {
            CustomCode = 400042;
            CustomMessage = localizer.GetString(CustomCode.ToString());
        }
    }
}
