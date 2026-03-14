using Microsoft.Extensions.Localization;

namespace APICore.Services.Exceptions
{
    public class SaleOrderCannotEditBadRequestException : BaseBadRequestException
    {
        public SaleOrderCannotEditBadRequestException(IStringLocalizer<object> localizer) : base()
        {
            CustomCode = 400043;
            CustomMessage = localizer.GetString(CustomCode.ToString());
        }
    }
}
