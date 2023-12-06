namespace LinkBot.Utility
{
    internal static class EnumerableExtensions
    {
        public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source)
            where T : class
        {
            return source.Where(x => x != null)!;
        }

        public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source)
            where T : struct
        {
            return source.Where(x => x.HasValue).Select(x => x!.Value);
        }
    }
}
