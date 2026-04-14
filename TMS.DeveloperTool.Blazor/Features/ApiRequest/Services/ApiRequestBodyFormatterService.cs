using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace TMS.DeveloperTool.Blazor.Features.ApiRequest.Services;

public sealed partial class ApiRequestBodyFormatterService
{
    [GeneratedRegex(@"^-?(0|[1-9]\d*)(\.\d+)?([eE][+-]?\d+)?$")]
    private static partial Regex JsonNumberPattern();

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public string NormalizeAndFormatJson(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        if (TryFormatStrictJson(raw, out string formattedStrictJson))
        {
            return formattedStrictJson;
        }

        int index = 0;
        string strictJson = ParseLooseJsonValue(raw, ref index);
        SkipWhitespace(raw, ref index);
        if (index < raw.Length)
        {
            throw new FormatException("Unexpected trailing content in request body.");
        }

        if (!TryFormatStrictJson(strictJson, out string formattedFromLooseJson))
        {
            throw new FormatException("Body cannot be converted to valid JSON.");
        }

        return formattedFromLooseJson;
    }

    private bool TryFormatStrictJson(string input, out string formatted)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(input);
            formatted = JsonSerializer.Serialize(document.RootElement, _jsonOptions);
            return true;
        }
        catch
        {
            formatted = string.Empty;
            return false;
        }
    }

    private static string ParseLooseJsonValue(string input, ref int index)
    {
        SkipWhitespace(input, ref index);
        if (index >= input.Length)
        {
            throw new FormatException("Unexpected end of input.");
        }

        char current = input[index];
        if (current == '{')
        {
            return ParseLooseJsonObject(input, ref index);
        }

        if (current == '[')
        {
            return ParseLooseJsonArray(input, ref index);
        }

        if (current is '\'' or '"')
        {
            string quoted = ReadQuotedText(input, ref index);
            return JsonSerializer.Serialize(quoted);
        }

        string bareToken = ReadBareToken(input, ref index);
        if (string.Equals(bareToken, "true", StringComparison.OrdinalIgnoreCase))
        {
            return "true";
        }

        if (string.Equals(bareToken, "false", StringComparison.OrdinalIgnoreCase))
        {
            return "false";
        }

        if (string.Equals(bareToken, "null", StringComparison.OrdinalIgnoreCase))
        {
            return "null";
        }

        if (IsJsonNumber(bareToken))
        {
            return bareToken;
        }

        return JsonSerializer.Serialize(bareToken);
    }

    private static string ParseLooseJsonObject(string input, ref int index)
    {
        StringBuilder builder = new();
        builder.Append('{');
        index++;

        SkipWhitespace(input, ref index);
        if (index < input.Length && input[index] == '}')
        {
            builder.Append('}');
            index++;
            return builder.ToString();
        }

        while (index < input.Length)
        {
            SkipWhitespace(input, ref index);
            string key = ReadObjectKey(input, ref index);
            builder.Append(JsonSerializer.Serialize(key));

            SkipWhitespace(input, ref index);
            if (index >= input.Length || input[index] != ':')
            {
                throw new FormatException("Expected ':' after object key.");
            }

            builder.Append(':');
            index++;
            builder.Append(ParseLooseJsonValue(input, ref index));

            SkipWhitespace(input, ref index);
            if (index < input.Length && input[index] == ',')
            {
                builder.Append(',');
                index++;
                continue;
            }

            if (index < input.Length && input[index] == '}')
            {
                builder.Append('}');
                index++;
                return builder.ToString();
            }

            throw new FormatException("Expected ',' or '}' in object.");
        }

        throw new FormatException("Unterminated object.");
    }

    private static string ParseLooseJsonArray(string input, ref int index)
    {
        StringBuilder builder = new();
        builder.Append('[');
        index++;

        SkipWhitespace(input, ref index);
        if (index < input.Length && input[index] == ']')
        {
            builder.Append(']');
            index++;
            return builder.ToString();
        }

        while (index < input.Length)
        {
            builder.Append(ParseLooseJsonValue(input, ref index));

            SkipWhitespace(input, ref index);
            if (index < input.Length && input[index] == ',')
            {
                builder.Append(',');
                index++;
                continue;
            }

            if (index < input.Length && input[index] == ']')
            {
                builder.Append(']');
                index++;
                return builder.ToString();
            }

            throw new FormatException("Expected ',' or ']' in array.");
        }

        throw new FormatException("Unterminated array.");
    }

    private static string ReadObjectKey(string input, ref int index)
    {
        if (index >= input.Length)
        {
            throw new FormatException("Unexpected end while reading object key.");
        }

        if (input[index] is '\'' or '"')
        {
            return ReadQuotedText(input, ref index);
        }

        int start = index;
        while (index < input.Length && input[index] != ':')
        {
            index++;
        }

        string key = input[start..index].Trim();
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new FormatException("Object key cannot be empty.");
        }

        return key;
    }

    private static string ReadQuotedText(string input, ref int index)
    {
        if (index >= input.Length)
        {
            throw new FormatException("Unexpected end while reading quoted text.");
        }

        char quote = input[index];
        index++;
        StringBuilder builder = new();

        while (index < input.Length)
        {
            char current = input[index];
            index++;

            if (current == '\\')
            {
                if (index >= input.Length)
                {
                    throw new FormatException("Invalid escape sequence.");
                }

                char escaped = input[index];
                index++;
                if (escaped == 'u')
                {
                    if (index + 4 > input.Length)
                    {
                        throw new FormatException("Invalid unicode escape sequence.");
                    }

                    string hex = input.Substring(index, 4);
                    if (!int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out int codePoint))
                    {
                        throw new FormatException("Invalid unicode escape sequence.");
                    }

                    builder.Append((char)codePoint);
                    index += 4;
                    continue;
                }

                builder.Append(escaped switch
                {
                    '\\' => '\\',
                    '/' => '/',
                    '"' => '"',
                    '\'' => '\'',
                    'b' => '\b',
                    'f' => '\f',
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    _ => escaped
                });
                continue;
            }

            if (current == quote)
            {
                return builder.ToString();
            }

            builder.Append(current);
        }

        throw new FormatException("Unterminated quoted text.");
    }

    private static string ReadBareToken(string input, ref int index)
    {
        int start = index;
        while (index < input.Length)
        {
            char current = input[index];
            if (current is ',' or '}' or ']')
            {
                break;
            }

            index++;
        }

        string token = input[start..index].Trim();
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new FormatException("Empty value is not valid.");
        }

        return token;
    }

    private static void SkipWhitespace(string input, ref int index)
    {
        while (index < input.Length && char.IsWhiteSpace(input[index]))
        {
            index++;
        }
    }

    private static bool IsJsonNumber(string value)
    {
        return JsonNumberPattern().IsMatch(value);
    }
}
