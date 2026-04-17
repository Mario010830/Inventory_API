using Microsoft.Extensions.Localization;

namespace APICore.Services.Exceptions
{
    public class PaymentMethodNotFoundException : BaseNotFoundException
    {
        public PaymentMethodNotFoundException(IStringLocalizer<object> localizer) : base()
        {
            CustomCode = 404045;
            CustomMessage = localizer.GetString(CustomCode.ToString());
        }
    }
}
