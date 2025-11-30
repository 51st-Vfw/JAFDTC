using JAFDTC.TacView.Models;

namespace JAFDTC.TacView.Extensions
{
    public static class FilterExtension
    {
        public static IEnumerable<UnitItem> LimitCoalitions(this IEnumerable<UnitItem> values, CoalitionType[]? coalitions)
        {
            if (coalitions == null || !coalitions.Any())
                return values;

            return values.Where(u => coalitions.Contains(u.Coalition));
        }

        public static IEnumerable<UnitItem> LimitCategories(this IEnumerable<UnitItem> values, CategoryType[]? categories)
        {
            if (categories == null || !categories.Any())
                return values;

            return values.Where(u => categories.Contains(u.Category));
        }

        public static IEnumerable<UnitItem> LimitAlive(this IEnumerable<UnitItem> values, bool? isAlive)
        {
            if (!isAlive.HasValue)
                return values;

            return values.Where(u => u.IsAlive == isAlive.Value);
        }
    }
}
