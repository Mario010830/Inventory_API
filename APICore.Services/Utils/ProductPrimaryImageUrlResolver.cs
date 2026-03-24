using APICore.Data.Entities;
using System.Linq;

namespace APICore.Services.Utils
{
    public static class ProductPrimaryImageUrlResolver
    {
        public static string Resolve(Product product)
        {
            if (product?.ProductImages == null || product.ProductImages.Count == 0)
                return product?.ImagenUrl ?? string.Empty;

            var ordered = product.ProductImages.OrderBy(pi => pi.SortOrder).ToList();
            var main = ordered.FirstOrDefault(pi => pi.IsMain);
            if (main != null && !string.IsNullOrEmpty(main.ImageUrl))
                return main.ImageUrl;

            var atZero = ordered.FirstOrDefault(pi => pi.SortOrder == 0);
            if (atZero != null && !string.IsNullOrEmpty(atZero.ImageUrl))
                return atZero.ImageUrl;

            return ordered.FirstOrDefault(pi => !string.IsNullOrEmpty(pi.ImageUrl))?.ImageUrl
                   ?? product.ImagenUrl ?? string.Empty;
        }
    }
}
