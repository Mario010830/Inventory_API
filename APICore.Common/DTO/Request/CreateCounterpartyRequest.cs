using System.Collections.Generic;

namespace APICore.Common.DTO.Request
{
    /// <summary>
    /// Alta unificada de contraparte. Roles en minúsculas: customer, supplier, lead.
    /// </summary>
    public class CreateCounterpartyRequest : CreateContactRequest
    {
        public List<string> Roles { get; set; } = new();
    }
}
