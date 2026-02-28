namespace APICore.Data.Entities
{
    public class Setting
    {
        public int Id { get; set; }
        /// <summary>Null = global setting (visible to all orgs), otherwise scoped to that organization.</summary>
        public int? OrganizationId { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
    }
}