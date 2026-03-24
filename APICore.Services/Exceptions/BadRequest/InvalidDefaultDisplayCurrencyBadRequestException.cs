namespace APICore.Services.Exceptions
{
    public class InvalidDefaultDisplayCurrencyBadRequestException : BaseBadRequestException
    {
        public InvalidDefaultDisplayCurrencyBadRequestException(string message)
        {
            CustomCode = 400454;
            CustomMessage = message;
        }
    }
}
