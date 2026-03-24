namespace APICore.Services.Exceptions
{
    public class InactivePlanBadRequestException : BaseBadRequestException
    {
        public InactivePlanBadRequestException()
        {
            CustomCode = 400405;
            CustomMessage = "El plan seleccionado no está disponible.";
        }
    }
}
