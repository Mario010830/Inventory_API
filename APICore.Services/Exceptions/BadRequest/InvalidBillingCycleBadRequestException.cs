namespace APICore.Services.Exceptions
{
    public class InvalidBillingCycleBadRequestException : BaseBadRequestException
    {
        public InvalidBillingCycleBadRequestException()
        {
            CustomCode = 400401;
            CustomMessage = "El ciclo de facturación debe ser \"monthly\" o \"annual\".";
        }
    }
}
