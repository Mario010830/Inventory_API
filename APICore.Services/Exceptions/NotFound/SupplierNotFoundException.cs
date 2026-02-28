using Microsoft.Extensions.Localization;

namespace APICore.Services.Exceptions
{
    public class SupplierNotFoundException : BaseNotFoundException
    {
        public SupplierNotFoundException(IStringLocalizer<object> localizer) : base()
        {
            CustomCode = 404006;
            CustomMessage = localizer.GetString(CustomCode.ToString());
        }
    }
}
