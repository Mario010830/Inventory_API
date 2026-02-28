using Microsoft.Extensions.Localization;

namespace APICore.Services.Exceptions
{
    public class CategoryHasProductsBadRequestException : BaseBadRequestException
    {
        public CategoryHasProductsBadRequestException(IStringLocalizer<object> localizer) : base()
        {
            CustomCode = 400009;
            CustomMessage = localizer.GetString(CustomCode.ToString());
        }
    }
}
