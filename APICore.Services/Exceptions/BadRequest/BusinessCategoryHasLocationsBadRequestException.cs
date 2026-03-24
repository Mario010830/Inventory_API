namespace APICore.Services.Exceptions
{
    public class BusinessCategoryHasLocationsBadRequestException : BaseBadRequestException
    {
        public BusinessCategoryHasLocationsBadRequestException(string message)
        {
            CustomCode = 400459;
            CustomMessage = message;
        }
    }
}
