namespace APICore.Services.Exceptions
{
    public class PlanNotFoundException : BaseNotFoundException
    {
        public PlanNotFoundException()
        {
            CustomCode = 404401;
            CustomMessage = "Plan no encontrado.";
        }
    }
}
