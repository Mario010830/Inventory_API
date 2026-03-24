namespace APICore.Services.Exceptions
{
    public class BusinessCategoryNotFoundException : BaseNotFoundException
    {
        public BusinessCategoryNotFoundException()
        {
            CustomCode = 404043;
            CustomMessage = "Categoría de negocio no encontrada.";
        }
    }
}
