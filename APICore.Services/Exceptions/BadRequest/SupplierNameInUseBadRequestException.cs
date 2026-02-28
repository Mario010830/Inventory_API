using Microsoft.Extensions.Localization;

namespace APICore.Services.Exceptions
{
    public class SupplierNameInUseBadRequestException : BaseBadRequestException
    {
        public SupplierNameInUseBadRequestException(IStringLocalizer<object> localizer) : base()
        {
            CustomCode = 400010;
            CustomMessage = localizer.GetString(CustomCode.ToString());
        }
    }
}
