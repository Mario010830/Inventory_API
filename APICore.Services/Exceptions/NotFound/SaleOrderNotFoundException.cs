using Microsoft.Extensions.Localization;

namespace APICore.Services.Exceptions
{
    public class SaleOrderNotFoundException : BaseNotFoundException
    {
        public SaleOrderNotFoundException(IStringLocalizer<object> localizer) : base()
        {
            CustomCode = 404040;
            CustomMessage = localizer.GetString(CustomCode.ToString());
        }
    }
}
