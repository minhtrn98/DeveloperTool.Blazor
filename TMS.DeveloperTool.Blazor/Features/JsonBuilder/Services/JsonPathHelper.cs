using System.Text.Json.Nodes;

namespace TMS.DeveloperTool.Blazor.Features.JsonBuilder.Services;

public static class JsonPathHelper
{
    /// <summary>
    /// Gan gia tri moi vao JsonNode theo duong dan (ho tro object path va array index).
    /// </summary>
    /// <param name="node">Node JSON goc can cap nhat.</param>
    /// <param name="path">Duong dan den truong can cap nhat. Vi du: user.name hoac items[0].price.</param>
    /// <param name="newValue">Gia tri moi se duoc ep kieu dua theo node hien tai.</param>
    /// <returns>
    /// True neu cap nhat thanh cong; false neu path khong hop le, index vuot pham vi,
    /// hoac khong tim thay node dich.
    /// </returns>
    /// <example>
    /// <code>
    /// var json = JsonNode.Parse("{\"user\":{\"name\":\"John\"}}")!;
    /// var ok = JsonPathHelper.TrySetValueByPath(json, "user.name", "Jane");
    /// // ok = true, user.name = "Jane"
    /// </code>
    /// </example>
    public static bool TrySetValueByPath(JsonNode node, string path, object newValue)
    {
        string[] parts = ParsePathParts(path);
        if (parts.Length == 0)
        {
            return false;
        }

        JsonNode? current = node;
        for (int i = 0; i < parts.Length - 1; i++)
        {
            string part = parts[i];
            if (int.TryParse(part, out int index))
            {
                if (current is not JsonArray array || index < 0 || index >= array.Count)
                {
                    return false;
                }

                current = array[index];
                continue;
            }

            if (current is not JsonObject obj || obj[part] is null)
            {
                return false;
            }

            current = obj[part];
        }

        if (current is null)
        {
            return false;
        }

        string lastPart = parts[^1];
        if (int.TryParse(lastPart, out int lastIndex))
        {
            if (current is not JsonArray lastArray || lastIndex < 0 || lastIndex >= lastArray.Count)
            {
                return false;
            }

            JsonNode? existingNode = lastArray[lastIndex];
            lastArray[lastIndex] = JsonNodeValueFactory.CreateTypedNode(existingNode, newValue);
            return true;
        }

        if (current is not JsonObject lastObject)
        {
            return false;
        }

        JsonNode? existingObjectNode = lastObject[lastPart];
        lastObject[lastPart] = JsonNodeValueFactory.CreateTypedNode(existingObjectNode, newValue);
        return true;
    }

    /// <summary>
    /// Tao path lien quan trong cung cap cha cua sourcePath.
    /// </summary>
    /// <param name="sourcePath">Path nguon. Vi du: user.profile.name.</param>
    /// <param name="relatedKeyName">Ten key moi can tao cung cap voi key nguon.</param>
    /// <returns>Path moi trong cung cap cha. Vi du: user.profile.email.</returns>
    /// <example>
    /// <code>
    /// var related = JsonPathHelper.BuildRelatedPath("items[0].name", "description");
    /// // related = "items[0].description"
    /// </code>
    /// </example>
    public static string BuildRelatedPath(string sourcePath, string relatedKeyName)
    {
        string parentPath = GetParentPath(sourcePath);
        if (string.IsNullOrEmpty(parentPath))
        {
            return relatedKeyName;
        }

        return $"{parentPath}.{relatedKeyName}";
    }

    private static string GetParentPath(string path)
    {
        int lastSeparatorIndex = path.LastIndexOf('.');
        if (lastSeparatorIndex < 0)
        {
            return string.Empty;
        }

        return path[..lastSeparatorIndex];
    }

    private static string[] ParsePathParts(string path)
    {
        return
        [
            .. path
                .Split(['.', '['], StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.TrimEnd(']'))
        ];
    }
}