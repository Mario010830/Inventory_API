using System;
using System.Collections.Generic;

namespace APICore.Common.DTO.Response
{
    public class OrganizationResponse
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }
        /// <summary>Localizaciones de la organización. Solo poblado en GET por id.</summary>
        public List<LocationResponse> Locations { get; set; } = new List<LocationResponse>();
    }
}
