using Microsoft.Extensions.Localization;

namespace APICore.Services.Exceptions
{
    public class ContactNotSupplierForMovementBadRequestException : BaseBadRequestException
    {
        public ContactNotSupplierForMovementBadRequestException(IStringLocalizer<object> localizer) : base()
        {
            CustomCode = 400512;
            CustomMessage = localizer.GetString(CustomCode.ToString());
        }
    }
}
