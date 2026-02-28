using APICore.Common.DTO;

namespace APICore.Services
{
    /// <summary>
    /// Proporciona acceso al contexto del usuario actual (quien implementa esto es quien "lee" el contexto, p. ej. desde HttpContext).
    /// El dato que devuelve es <see cref="CurrentUserContext"/>.
    /// </summary>
    public interface ICurrentUserContextAccessor
    {
        CurrentUserContext? GetCurrent();
    }
}
