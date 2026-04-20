namespace APICore.Services.Exceptions
{
    public class InvalidMobileListLayoutBadRequestException : BaseBadRequestException
    {
        public InvalidMobileListLayoutBadRequestException()
        {
            CustomCode = 400478;
            CustomMessage = "El diseño móvil debe ser \"table\", \"comfortable\" o vacío para restablecer.";
        }
    }
}
