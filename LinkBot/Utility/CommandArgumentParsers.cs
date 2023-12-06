using System.Collections.Immutable;

namespace LinkBot.Utility
{
    public static class CommandArgumentParsers
    {
        public static IImmutableSet<int> ParseMediaPositions(string selected, string excluded, int maxCount)
        {
            if (!string.IsNullOrEmpty(selected))
            {
                return selected
                    .Split(new[] { ' ', ',' })
                    .Select(x => int.Parse(x.Trim()))
                    .Take(maxCount)
                    .ToImmutableHashSet();
            }
            else if (!string.IsNullOrEmpty(excluded))
            {
                var parsed = excluded
                    .Split(new[] { ' ', ',' })
                    .Select(x => int.Parse(x.Trim()))
                    .Take(maxCount)
                    .ToImmutableHashSet();

                return Enumerable.Range(1, maxCount + parsed.Count)
                    .Except(parsed)
                    .Take(maxCount)
                    .ToImmutableHashSet();
            }
            else
            {
                return Enumerable.Range(1, maxCount).ToImmutableHashSet();
            }
        }
    }
}
