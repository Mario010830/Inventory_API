using Microsoft.Extensions.Localization;

namespace APICore.Services.Exceptions
{
    public class InventoryMovementNotFoundException : BaseNotFoundException
    {
        public InventoryMovementNotFoundException(IStringLocalizer<object> localizer) : base()
        {
            CustomCode = 404005;
            CustomMessage = localizer.GetString(CustomCode.ToString());
        }
    }
}
