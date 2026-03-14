using Microsoft.Extensions.Localization;

namespace APICore.Services.Exceptions
{
    public class SaleReturnNotFoundException : BaseNotFoundException
    {
        public SaleReturnNotFoundException(IStringLocalizer<object> localizer) : base()
        {
            CustomCode = 404041;
            CustomMessage = localizer.GetString(CustomCode.ToString());
        }
    }
}
