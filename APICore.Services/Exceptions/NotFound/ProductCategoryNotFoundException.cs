using Microsoft.Extensions.Localization;

namespace APICore.Services.Exceptions
{
    public class ProductCategoryNotFoundException : BaseNotFoundException
    {
        public ProductCategoryNotFoundException(IStringLocalizer<object> localizer) : base()
        {
            CustomCode = 404003;
            CustomMessage = localizer.GetString(CustomCode.ToString());
        }
    }
}
