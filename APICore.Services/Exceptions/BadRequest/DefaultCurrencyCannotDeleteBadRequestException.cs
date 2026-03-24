namespace APICore.Services.Exceptions
{
    public class DefaultCurrencyCannotDeleteBadRequestException : BaseBadRequestException
    {
        public DefaultCurrencyCannotDeleteBadRequestException()
        {
            CustomCode = 400453;
            CustomMessage = "No se puede eliminar la moneda configurada como predeterminada para mostrar. Asigne otra moneda predeterminada antes de eliminar.";
        }
    }
}
