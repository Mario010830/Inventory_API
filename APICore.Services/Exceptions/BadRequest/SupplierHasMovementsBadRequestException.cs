using Microsoft.Extensions.Localization;

namespace APICore.Services.Exceptions
{
    public class SupplierHasMovementsBadRequestException : BaseBadRequestException
    {
        public SupplierHasMovementsBadRequestException(IStringLocalizer<object> localizer) : base()
        {
            CustomCode = 400011;
            CustomMessage = localizer.GetString(CustomCode.ToString());
        }
    }
}
