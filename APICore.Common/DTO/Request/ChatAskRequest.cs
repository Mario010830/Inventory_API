using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace APICore.Common.DTO.Request
{
    public class ChatAskRequest
    {
        /// <summary>Pregunta del usuario (máx. 500 caracteres por configuración de API).</summary>
        [Required]
        [MaxLength(500)]
        public string Question { get; set; } = string.Empty;

        /// <summary>Historial opcional (roles user / assistant).</summary>
        public List<ChatConversationTurnDto>? ConversationHistory { get; set; }
    }

    public class ChatConversationTurnDto
    {
        [Required]
        public string Role { get; set; } = string.Empty;

        [Required]
        [MaxLength(8000)]
        public string Content { get; set; } = string.Empty;
    }
}
