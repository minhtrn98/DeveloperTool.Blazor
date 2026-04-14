using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace TMS.DeveloperTool.Blazor.Features.ApiRequest.Services;

public sealed class ApiRequestSwaggerService(IWebHostEnvironment environment)
{
    private static readonly string[] PreferredJsonContentTypes = [
        "application/json",
        "text/json"
    ];

    public async Task<SwaggerDocumentInfo?> LoadAsync(string serviceName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(serviceName))
        {
            return null;
        }

        string filePath = Path.Combine(environment.ContentRootPath, "Templates", $"Swagger.{serviceName}.json");
        if (!File.Exists(filePath))
        {
            return null;
        }

        await using FileStream stream = File.OpenRead(filePath);
        using JsonDocument document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        return Parse(serviceName, document.RootElement);
    }

    public SwaggerDocumentInfo Parse(string serviceName, string swaggerJson)
    {
        using JsonDocument document = JsonDocument.Parse(swaggerJson);
        return Parse(serviceName, document.RootElement);
    }

    private SwaggerDocumentInfo Parse(string serviceName, JsonElement root)
    {
        Dictionary<string, JsonElement> schemas = GetSchemas(root);
        List<SwaggerEndpointOption> endpoints = [];

        string title = root.TryGetProperty("info", out JsonElement info)
            && info.TryGetProperty("title", out JsonElement titleElement)
            ? titleElement.GetString() ?? serviceName
            : serviceName;

        if (root.TryGetProperty("paths", out JsonElement paths) && paths.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty pathProperty in paths.EnumerateObject())
            {
                JsonElement pathItem = pathProperty.Value;
                foreach (JsonProperty operationProperty in pathItem.EnumerateObject())
                {
                    string method = operationProperty.Name.ToUpperInvariant();
                    if (!IsHttpMethod(method))
                    {
                        continue;
                    }

                    JsonElement operation = operationProperty.Value;
                    List<SwaggerParameterOption> parameters = ExtractParameters(pathItem, operation, schemas);
                    string path = BuildEndpointPath(pathProperty.Name, parameters);
                    string tag = ExtractTag(operation);
                    string description = ExtractDescription(operation);
                    string sampleRequestBody = ExtractRequestBodySample(operation, schemas);

                    endpoints.Add(new SwaggerEndpointOption(
                        Key: $"{method}:{pathProperty.Name}",
                        DisplayName: $"{method} {pathProperty.Name}",
                        Method: method,
                        Path: path,
                        Tag: tag,
                        Description: description,
                        SampleRequestBody: sampleRequestBody,
                        Parameters: parameters));
                }
            }
        }

        List<string> tags = endpoints
            .Select(x => x.Tag)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new SwaggerDocumentInfo(serviceName, title, tags, endpoints
            .OrderBy(x => x.Tag, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Path, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Method, StringComparer.OrdinalIgnoreCase)
            .ToList());
    }

    private static Dictionary<string, JsonElement> GetSchemas(JsonElement root)
    {
        Dictionary<string, JsonElement> schemas = new(StringComparer.Ordinal);
        if (root.TryGetProperty("components", out JsonElement components)
            && components.TryGetProperty("schemas", out JsonElement schemaRoot)
            && schemaRoot.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty schema in schemaRoot.EnumerateObject())
            {
                schemas[schema.Name] = schema.Value;
            }
        }

        if (root.TryGetProperty("definitions", out JsonElement definitions)
            && definitions.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty schema in definitions.EnumerateObject())
            {
                schemas[schema.Name] = schema.Value;
            }
        }

        return schemas;
    }

    private static List<SwaggerParameterOption> ExtractParameters(
        JsonElement pathItem,
        JsonElement operation,
        Dictionary<string, JsonElement> schemas)
    {
        Dictionary<string, SwaggerParameterOption> result = new(StringComparer.OrdinalIgnoreCase);

        AddParameters(pathItem, result, schemas);
        AddParameters(operation, result, schemas);

        return result.Values
            .OrderBy(x => x.Location, StringComparer.OrdinalIgnoreCase)
            .ThenByDescending(x => x.Required)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void AddParameters(
        JsonElement owner,
        Dictionary<string, SwaggerParameterOption> result,
        Dictionary<string, JsonElement> schemas)
    {
        if (!owner.TryGetProperty("parameters", out JsonElement parameters) || parameters.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (JsonElement parameter in parameters.EnumerateArray())
        {
            JsonElement resolvedParameter = ResolveReference(parameter, schemas, []);
            if (resolvedParameter.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            string name = resolvedParameter.TryGetProperty("name", out JsonElement nameElement)
                ? nameElement.GetString() ?? string.Empty
                : string.Empty;
            string location = resolvedParameter.TryGetProperty("in", out JsonElement inElement)
                ? inElement.GetString() ?? string.Empty
                : string.Empty;
            bool required = resolvedParameter.TryGetProperty("required", out JsonElement requiredElement)
                && requiredElement.ValueKind == JsonValueKind.True;

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(location))
            {
                continue;
            }

            JsonElement schema = GetParameterSchema(resolvedParameter);
            object? sample = TryGetExampleValue(resolvedParameter, schemas, [])
                ?? (schema.ValueKind != JsonValueKind.Undefined ? BuildSampleFromSchema(schema, schemas, [], 0) : null);

            result[$"{location}:{name}"] = new SwaggerParameterOption(
                Name: name,
                Location: location,
                Required: required,
                SampleValue: ConvertSampleToString(sample, name));
        }
    }

    private static string BuildEndpointPath(string originalPath, IReadOnlyList<SwaggerParameterOption> parameters)
    {
        string path = originalPath;

        foreach (SwaggerParameterOption parameter in parameters.Where(x => x.Location.Equals("path", StringComparison.OrdinalIgnoreCase)))
        {
            path = path.Replace($"{{{parameter.Name}}}", Uri.EscapeDataString(parameter.SampleValue), StringComparison.Ordinal);
        }

        List<string> querySegments = parameters
            .Where(x => x.Location.Equals("query", StringComparison.OrdinalIgnoreCase))
            .Select(x => $"{Uri.EscapeDataString(x.Name)}={Uri.EscapeDataString(x.SampleValue)}")
            .ToList();

        if (querySegments.Count == 0)
        {
            return path;
        }

        string separator = path.Contains('?') ? "&" : "?";
        return $"{path}{separator}{string.Join("&", querySegments)}";
    }

    private static string ExtractTag(JsonElement operation)
    {
        if (operation.TryGetProperty("tags", out JsonElement tags) && tags.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement tag in tags.EnumerateArray())
            {
                string? value = tag.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }
        }

        return "General";
    }

    private static string ExtractDescription(JsonElement operation)
    {
        if (operation.TryGetProperty("summary", out JsonElement summaryElement))
        {
            return summaryElement.GetString() ?? string.Empty;
        }

        if (operation.TryGetProperty("description", out JsonElement descriptionElement))
        {
            return descriptionElement.GetString() ?? string.Empty;
        }

        return string.Empty;
    }

    private static string ExtractRequestBodySample(JsonElement operation, Dictionary<string, JsonElement> schemas)
    {
        if (operation.TryGetProperty("requestBody", out JsonElement requestBody))
        {
            return ExtractOpenApiRequestBodySample(requestBody, schemas);
        }

        if (operation.TryGetProperty("parameters", out JsonElement parameters) && parameters.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement parameter in parameters.EnumerateArray())
            {
                JsonElement resolvedParameter = ResolveReference(parameter, schemas, []);
                if (!resolvedParameter.TryGetProperty("in", out JsonElement inElement)
                    || !string.Equals(inElement.GetString(), "body", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!resolvedParameter.TryGetProperty("schema", out JsonElement schema))
                {
                    continue;
                }

                object? sample = TryGetExampleValue(resolvedParameter, schemas, [])
                    ?? BuildSampleFromSchema(schema, schemas, [], 0);
                return SerializeSample(sample);
            }
        }

        return string.Empty;
    }

    private static string ExtractOpenApiRequestBodySample(JsonElement requestBody, Dictionary<string, JsonElement> schemas)
    {
        if (!requestBody.TryGetProperty("content", out JsonElement content) || content.ValueKind != JsonValueKind.Object)
        {
            return string.Empty;
        }

        JsonElement mediaType = default;
        bool found = false;

        foreach (string preferredContentType in PreferredJsonContentTypes)
        {
            if (content.TryGetProperty(preferredContentType, out mediaType))
            {
                found = true;
                break;
            }
        }

        if (!found)
        {
            foreach (JsonProperty item in content.EnumerateObject())
            {
                if (item.Name.Contains("json", StringComparison.OrdinalIgnoreCase))
                {
                    mediaType = item.Value;
                    found = true;
                    break;
                }
            }
        }

        if (!found)
        {
            mediaType = content.EnumerateObject().FirstOrDefault().Value;
            found = mediaType.ValueKind != JsonValueKind.Undefined;
        }

        if (!found)
        {
            return string.Empty;
        }

        object? sample = TryGetExampleValue(mediaType, schemas, []);
        if (sample is null && mediaType.TryGetProperty("schema", out JsonElement schema))
        {
            sample = BuildSampleFromSchema(schema, schemas, [], 0);
        }

        return SerializeSample(sample);
    }

    private static object? BuildSampleFromSchema(
        JsonElement schema,
        Dictionary<string, JsonElement> schemas,
        HashSet<string> resolvingReferences,
        int depth)
    {
        if (depth > 12)
        {
            return null;
        }

        JsonElement resolvedSchema = ResolveReference(schema, schemas, resolvingReferences);
        if (TryGetExampleValue(resolvedSchema, schemas, resolvingReferences) is { } exampleValue)
        {
            return exampleValue;
        }

        if (resolvedSchema.TryGetProperty("allOf", out JsonElement allOf) && allOf.ValueKind == JsonValueKind.Array)
        {
            Dictionary<string, object?> merged = new(StringComparer.Ordinal);
            foreach (JsonElement part in allOf.EnumerateArray())
            {
                object? partSample = BuildSampleFromSchema(part, schemas, resolvingReferences, depth + 1);
                if (partSample is Dictionary<string, object?> map)
                {
                    foreach ((string key, object? value) in map)
                    {
                        merged[key] = value;
                    }
                }
            }

            return merged.Count > 0 ? merged : null;
        }

        if (resolvedSchema.TryGetProperty("oneOf", out JsonElement oneOf) && oneOf.ValueKind == JsonValueKind.Array)
        {
            JsonElement first = oneOf.EnumerateArray().FirstOrDefault();
            return first.ValueKind == JsonValueKind.Undefined
                ? null
                : BuildSampleFromSchema(first, schemas, resolvingReferences, depth + 1);
        }

        if (resolvedSchema.TryGetProperty("anyOf", out JsonElement anyOf) && anyOf.ValueKind == JsonValueKind.Array)
        {
            JsonElement first = anyOf.EnumerateArray().FirstOrDefault();
            return first.ValueKind == JsonValueKind.Undefined
                ? null
                : BuildSampleFromSchema(first, schemas, resolvingReferences, depth + 1);
        }

        if (resolvedSchema.TryGetProperty("enum", out JsonElement enumElement) && enumElement.ValueKind == JsonValueKind.Array)
        {
            JsonElement first = enumElement.EnumerateArray().FirstOrDefault();
            return ConvertJsonElement(first);
        }

        string? type = resolvedSchema.TryGetProperty("type", out JsonElement typeElement)
            ? typeElement.GetString()
            : null;

        if (type is null && resolvedSchema.TryGetProperty("properties", out _))
        {
            type = "object";
        }

        return type switch
        {
            "object" => BuildObjectSample(resolvedSchema, schemas, resolvingReferences, depth + 1),
            "array" => BuildArraySample(resolvedSchema, schemas, resolvingReferences, depth + 1),
            "boolean" => false,
            "integer" => BuildNumericSample(resolvedSchema, wholeNumber: true),
            "number" => BuildNumericSample(resolvedSchema, wholeNumber: false),
            "string" => BuildStringSample(resolvedSchema),
            _ => resolvedSchema.ValueKind == JsonValueKind.Object
                ? BuildObjectSample(resolvedSchema, schemas, resolvingReferences, depth + 1)
                : null
        };
    }

    private static Dictionary<string, object?> BuildObjectSample(
        JsonElement schema,
        Dictionary<string, JsonElement> schemas,
        HashSet<string> resolvingReferences,
        int depth)
    {
        Dictionary<string, object?> result = new(StringComparer.Ordinal);

        if (schema.TryGetProperty("properties", out JsonElement properties) && properties.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty property in properties.EnumerateObject())
            {
                result[property.Name] = BuildSampleFromSchema(property.Value, schemas, resolvingReferences, depth + 1);
            }
        }

        if (result.Count == 0 && schema.TryGetProperty("additionalProperties", out JsonElement additionalProperties))
        {
            result["additionalProp1"] = additionalProperties.ValueKind == JsonValueKind.Object
                ? BuildSampleFromSchema(additionalProperties, schemas, resolvingReferences, depth + 1)
                : "string";
        }

        return result;
    }

    private static List<object?> BuildArraySample(
        JsonElement schema,
        Dictionary<string, JsonElement> schemas,
        HashSet<string> resolvingReferences,
        int depth)
    {
        if (!schema.TryGetProperty("items", out JsonElement items))
        {
            return [];
        }

        return [BuildSampleFromSchema(items, schemas, resolvingReferences, depth + 1)];
    }

    private static object BuildNumericSample(JsonElement schema, bool wholeNumber)
    {
        if (schema.TryGetProperty("minimum", out JsonElement minimum))
        {
            if (wholeNumber && minimum.TryGetInt64(out long integerMinimum))
            {
                return integerMinimum;
            }

            if (!wholeNumber && minimum.TryGetDouble(out double doubleMinimum))
            {
                return doubleMinimum;
            }
        }

        return wholeNumber ? 0 : 0d;
    }

    private static string BuildStringSample(JsonElement schema)
    {
        string? format = schema.TryGetProperty("format", out JsonElement formatElement)
            ? formatElement.GetString()
            : null;

        return format switch
        {
            "uuid" => "00000000-0000-0000-0000-000000000000",
            "date" => DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            "date-time" => "2026-01-01T00:00:00Z",
            "time" => "08:00:00",
            "email" => "user@example.com",
            "uri" => "https://example.com",
            _ => "string"
        };
    }

    private static object? TryGetExampleValue(
        JsonElement element,
        Dictionary<string, JsonElement> schemas,
        HashSet<string> resolvingReferences)
    {
        if (element.TryGetProperty("example", out JsonElement example))
        {
            return ConvertJsonElement(example);
        }

        if (element.TryGetProperty("default", out JsonElement defaultValue))
        {
            return ConvertJsonElement(defaultValue);
        }

        if (element.TryGetProperty("examples", out JsonElement examples) && examples.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty exampleProperty in examples.EnumerateObject())
            {
                JsonElement exampleContainer = exampleProperty.Value;
                if (exampleContainer.TryGetProperty("value", out JsonElement value))
                {
                    return ConvertJsonElement(value);
                }
            }
        }

        if (element.TryGetProperty("schema", out JsonElement nestedSchema) && nestedSchema.ValueKind == JsonValueKind.Object)
        {
            JsonElement resolvedSchema = ResolveReference(nestedSchema, schemas, resolvingReferences);
            if (resolvedSchema.ValueKind == JsonValueKind.Object)
            {
                return TryGetExampleValue(resolvedSchema, schemas, resolvingReferences);
            }
        }

        return null;
    }

    private static JsonElement ResolveReference(
        JsonElement element,
        Dictionary<string, JsonElement> schemas,
        HashSet<string> resolvingReferences)
    {
        if (!element.TryGetProperty("$ref", out JsonElement referenceElement))
        {
            return element;
        }

        string? reference = referenceElement.GetString();
        if (string.IsNullOrWhiteSpace(reference))
        {
            return element;
        }

        string schemaName = reference.Split('/').Last();
        if (!schemas.TryGetValue(schemaName, out JsonElement resolved) || !resolvingReferences.Add(schemaName))
        {
            return element;
        }

        try
        {
            return ResolveReference(resolved, schemas, resolvingReferences);
        }
        finally
        {
            resolvingReferences.Remove(schemaName);
        }
    }

    private static JsonElement GetParameterSchema(JsonElement parameter)
    {
        if (parameter.TryGetProperty("schema", out JsonElement schema))
        {
            return schema;
        }

        return parameter;
    }

    private static object? ConvertJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => element.EnumerateObject().ToDictionary(x => x.Name, x => ConvertJsonElement(x.Value), StringComparer.Ordinal),
            JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonElement).ToList(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number when element.TryGetInt64(out long int64Value) => int64Value,
            JsonValueKind.Number when element.TryGetDecimal(out decimal decimalValue) => decimalValue,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.ToString()
        };
    }

    private static string SerializeSample(object? sample)
    {
        if (sample is null)
        {
            return string.Empty;
        }

        return JsonSerializer.Serialize(sample, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
    }

    private static string ConvertSampleToString(object? sample, string fallbackName)
    {
        return sample switch
        {
            null => fallbackName,
            string stringValue when !string.IsNullOrWhiteSpace(stringValue) => stringValue,
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => JsonSerializer.Serialize(sample)
        };
    }

    private static bool IsHttpMethod(string method)
    {
        return method is "GET" or "POST" or "PUT" or "DELETE" or "PATCH" or "HEAD" or "OPTIONS";
    }
}

public sealed record SwaggerDocumentInfo(
    string ServiceName,
    string Title,
    IReadOnlyList<string> Tags,
    IReadOnlyList<SwaggerEndpointOption> Endpoints);

public sealed record SwaggerEndpointOption(
    string Key,
    string DisplayName,
    string Method,
    string Path,
    string Tag,
    string Description,
    string SampleRequestBody,
    IReadOnlyList<SwaggerParameterOption> Parameters);

public sealed record SwaggerParameterOption(
    string Name,
    string Location,
    bool Required,
    string SampleValue);