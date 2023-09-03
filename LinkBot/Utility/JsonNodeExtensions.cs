using System.Text.Json.Nodes;

namespace LinkBot.Utility
{
    internal static class JsonNodeExtensions
    {
        public static JsonNode? FirstDescendantOrDefault(this JsonNode node, Func<JsonNode, string?, bool> predicate) =>
            node.FirstDescendantOrDefault(predicate, null);

        private static JsonNode? FirstDescendantOrDefault(this JsonNode node, Func<JsonNode, string?, bool> predicate, string? key)
        {
            if (predicate(node, key))
                return node;

            if (node is JsonArray array)
            {
                foreach (var child in array)
                {
                    var result = child?.FirstDescendantOrDefault(predicate, null);
                    if (result is not null)
                        return result;
                }
            }
            else if (node is JsonObject obj)
            {
                foreach (var child in obj)
                {
                    var result = child.Value?.FirstDescendantOrDefault(predicate, child.Key);
                    if (result is not null)
                        return result;
                }
            }

            return null;
        }
    }
}
