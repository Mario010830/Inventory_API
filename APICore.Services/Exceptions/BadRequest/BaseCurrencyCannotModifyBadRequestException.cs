namespace APICore.Services.Exceptions
{
    public class BaseCurrencyCannotModifyBadRequestException : BaseBadRequestException
    {
        public BaseCurrencyCannotModifyBadRequestException()
        {
            CustomCode = 400451;
            CustomMessage = "La moneda base (CUP) no se puede modificar.";
        }
    }
}
