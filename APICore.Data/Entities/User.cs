using APICore.Data.Entities.Enums;
using System;
using System.Collections.Generic;

namespace APICore.Data.Entities
{
    public class User : BaseEntity
    {
        public User()
        {
            UserTokens = new HashSet<UserToken>();
        }

    
        public DateTime BirthDate { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Password { get; set; }
        public string? GoogleId { get; set; }
        public StatusEnum Status { get; set; }
        public DateTimeOffset? LastLoggedIn { get; set; }
        public int? LocationId { get; set; }
        public int? OrganizationId { get; set; }
        public int? RoleId { get; set; }

        /// <summary>Vista preferida para listas en móvil: "table" | "comfortable"; null = sin elegir (mostrar asistente).</summary>
        public string? MobileListLayout { get; set; }

        /// <summary>Salario (opcional; uso interno de la organización).</summary>
        public decimal? Salary { get; set; }

        public virtual Location? Location { get; set; }
        public virtual Organization? Organization { get; set; }
        public virtual Role? Role { get; set; }
        public virtual ICollection<UserToken> UserTokens { get; set; }
    }
}