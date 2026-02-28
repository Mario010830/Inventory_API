using Microsoft.Extensions.Localization;

namespace APICore.Services.Exceptions
{
    public class ProductNotFoundException : BaseNotFoundException
    {
        public ProductNotFoundException(IStringLocalizer<object> localizer) : base()
        {
            CustomCode = 404002;
            CustomMessage = localizer.GetString(CustomCode.ToString());
        }
    }
}
