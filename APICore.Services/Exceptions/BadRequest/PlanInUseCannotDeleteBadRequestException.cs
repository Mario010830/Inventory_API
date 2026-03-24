namespace APICore.Services.Exceptions
{
    public class PlanInUseCannotDeleteBadRequestException : BaseBadRequestException
    {
        public PlanInUseCannotDeleteBadRequestException()
        {
            CustomCode = 400403;
            CustomMessage = "No se puede eliminar el plan porque tiene suscripciones asociadas.";
        }
    }
}
