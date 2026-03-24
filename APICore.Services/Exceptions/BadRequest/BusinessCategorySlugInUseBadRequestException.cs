namespace APICore.Services.Exceptions
{
    public class BusinessCategorySlugInUseBadRequestException : BaseBadRequestException
    {
        public BusinessCategorySlugInUseBadRequestException(string message)
        {
            CustomCode = 400458;
            CustomMessage = message;
        }
    }
}
