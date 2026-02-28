namespace APICore.Common.DTO.Response
{
    /// <summary>
    /// KPIs de la vista de categorías.
    /// </summary>
    public class CategoryStatsResponse
    {
        public int TotalCategories { get; set; }
        public string MostActiveCategoryName { get; set; } = string.Empty;
        public string LastEditedAgo { get; set; } = string.Empty;
        public int TotalItems { get; set; }
    }
}
