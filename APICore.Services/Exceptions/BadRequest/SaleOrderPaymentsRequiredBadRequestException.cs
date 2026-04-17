using Microsoft.Extensions.Localization;

namespace APICore.Services.Exceptions
{
    public class SaleOrderPaymentsRequiredBadRequestException : BaseBadRequestException
    {
        public SaleOrderPaymentsRequiredBadRequestException(IStringLocalizer<object> localizer) : base()
        {
            CustomCode = 400467;
            CustomMessage = localizer.GetString(CustomCode.ToString());
        }
    }
}
