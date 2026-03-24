namespace APICore.Services.Exceptions
{
    /// <summary>
    /// 400 Bad Request: no se puede eliminar la etiqueta porque tiene productos asignados.
    /// </summary>
    public class TagHasProductsBadRequestException : BaseBadRequestException
    {
        public TagHasProductsBadRequestException(string message)
        {
            CustomMessage = message;
        }
    }
}
