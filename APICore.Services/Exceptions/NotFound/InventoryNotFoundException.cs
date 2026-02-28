using Microsoft.Extensions.Localization;

namespace APICore.Services.Exceptions
{
    public class InventoryNotFoundException : BaseNotFoundException
    {
        public InventoryNotFoundException(IStringLocalizer<object> localizer) : base()
        {
            CustomCode = 404004;
            CustomMessage = localizer.GetString(CustomCode.ToString());
        }
    }
}
