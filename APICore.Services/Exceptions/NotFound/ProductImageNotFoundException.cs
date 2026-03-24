using Microsoft.Extensions.Localization;

namespace APICore.Services.Exceptions
{
    public class ProductImageNotFoundException : BaseNotFoundException
    {
        public ProductImageNotFoundException(IStringLocalizer<object> localizer) : base()
        {
            CustomCode = 404404;
            CustomMessage = localizer.GetString(CustomCode.ToString());
        }
    }
}
