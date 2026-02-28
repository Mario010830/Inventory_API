using Microsoft.Extensions.Localization;

namespace APICore.Services.Exceptions
{
    public class ProductCategoryNameInUseBadRequestException : BaseBadRequestException
    {
        public ProductCategoryNameInUseBadRequestException(IStringLocalizer<object> localizer) : base()
        {
            CustomCode = 400008;
            CustomMessage = localizer.GetString(CustomCode.ToString());
        }
    }
}
