namespace APICore.Services.Exceptions
{
    public class BaseCurrencyCannotDeleteBadRequestException : BaseBadRequestException
    {
        public BaseCurrencyCannotDeleteBadRequestException()
        {
            CustomCode = 400452;
            CustomMessage = "La moneda base (CUP) no se puede eliminar.";
        }
    }
}
