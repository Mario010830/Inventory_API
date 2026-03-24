namespace APICore.Services.Exceptions
{
    public class CurrencyCodeInUseBadRequestException : BaseBadRequestException
    {
        public CurrencyCodeInUseBadRequestException()
        {
            CustomCode = 400450;
            CustomMessage = "Ya existe una moneda con ese código.";
        }
    }
}
