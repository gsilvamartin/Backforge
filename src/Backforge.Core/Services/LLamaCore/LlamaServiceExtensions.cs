using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Backforge.Core.Services.LLamaCore;

public static class LlamaServiceExtensions
{
    private static readonly JsonSerializerOptions DefaultJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        Converters = { new JsonStringEnumConverter() },
        AllowTrailingCommas = false, // Keep strict parsing for production
        ReadCommentHandling = JsonCommentHandling.Skip,
        MaxDepth = 64
    };

    public static async Task<T> GetStructuredResponseAsync<T>(
        this ILlamaService llamaService,
        string prompt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(llamaService);
        ArgumentNullException.ThrowIfNull(prompt);

        var responseType = typeof(T);
        var isCollectionType = IsCollectionType(responseType);
        var elementType = isCollectionType ? GetCollectionElementType(responseType) : responseType;

        var jsonExample = BuildJsonExample(elementType);
        if (isCollectionType)
        {
            jsonExample = $"[{jsonExample}]";
        }

        var structuredPrompt = $$"""
                                 IMPORTANT INSTRUCTIONS – STRICT COMPLIANCE REQUIRED:

                                 1. Generate a response **STRICTLY** in the JSON format specified below.  
                                 2. **DO NOT** include any explanatory text, metadata, or additional comments before or after the JSON.  
                                 3. The output **MUST** match the exact structure and field names provided, without modifications.  
                                 4. **REQUIREMENTS:**  
                                    - **JSON format must be 100% valid and properly structured.**  
                                    - **Arrays must contain ONLY the expected type** (strings or objects, as specified).  
                                    - **No extra fields, no missing fields, no variations in structure.**  
                                    - **Any deviation will result in an invalid response.**  
                                    - **Do NOT include any text outside the JSON braces `{}` or brackets `[]`.**  
                                    - **If we have a json object in a string it must be properly ESCAPED.**

                                 EXPECTED RESPONSE TYPE: **{{(isCollectionType ? "ARRAY" : "OBJECT")}}**  
                                 EXPECTED ELEMENT TYPE: **{{elementType.Name}}**  

                                 ---- JSON MODEL ----
                                 {{jsonExample}}

                                 ---- REQUESTED DATA ----
                                 {{prompt}}

                                 FINAL REMINDER:  
                                 - **Your response must be a well-formed JSON matching the expected type EXACTLY.**  
                                 - **Failure to comply with these instructions will result in rejection of the response.**  
                                 """;

        var response = await llamaService.GetLlamaResponseAsync(structuredPrompt, cancellationToken);

        try
        {
            var cleanJson = CleanAndValidateJsonResponse(response, isCollectionType);
            return JsonSerializer.Deserialize<T>(cleanJson, DefaultJsonOptions) ??
                   throw new InvalidOperationException("Deserialization returned null");
        }
        catch (Exception ex)
        {
            try
            {
                var fallbackPrompt = $$"""
                                       Please fix the JSON response

                                       IMPORTANT INSTRUCTIONS – STRICT COMPLIANCE REQUIRED:

                                       1. Generate a response **STRICTLY** in the JSON format specified below.  
                                       2. **DO NOT** include any explanatory text, metadata, or additional comments before or after the JSON.  
                                       3. The output **MUST** match the exact structure and field names provided, without modifications.  
                                       4. **REQUIREMENTS:**  
                                          - **JSON format must be 100% valid and properly structured.**  
                                          - **Arrays must contain ONLY the expected type** (strings or objects, as specified).  
                                          - **No extra fields, no missing fields, no variations in structure.**  
                                          - **Any deviation will result in an invalid response.**  
                                          - **Do NOT include any text outside the JSON braces `{}` or brackets `[]`.**  

                                       ORIGINAL JSON WITH ERROR:
                                       {{response}}

                                       ERROR:
                                       {{ex.Message}}

                                       EXPECTED MODEL: 
                                       {{jsonExample}}
                                       """;

                var newResponse = await llamaService.GetLlamaResponseAsync(fallbackPrompt, cancellationToken);
                var cleanJson = CleanAndValidateJsonResponse(newResponse, isCollectionType);

                return JsonSerializer.Deserialize<T>(cleanJson, DefaultJsonOptions) ??
                       throw new InvalidOperationException("Deserialization returned null");
            }
            catch (Exception ex2)
            {
                throw new InvalidOperationException(
                    $"Failed to parse response as {typeof(T).Name}. " +
                    $"Response: {response}. " +
                    $"Expected model: {jsonExample}", ex2);
            }
        }
    }

    public static async Task<string> GenerateCodeAsync(
        this ILlamaService llamaService,
        string prompt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(llamaService);
        ArgumentNullException.ThrowIfNull(prompt);

        var codeGenerationPrompt = $"""
                                    IMPORTANT INSTRUCTIONS - STRICT COMPLIANCE REQUIRED:

                                    1. Generate ONLY the requested code without adding ANY additional explanation.
                                    2. DO NOT include code block markers like ``` or any other text beyond the code.
                                    3. The code must be complete, functional, and follow best practices for the language in question.
                                    4. Include all necessary imports/includes at the beginning of the file.
                                    5. DO NOT add introductory or explanatory comments before or after the code.

                                    Remember: Your response should contain ONLY clean source code, as if you were writing directly to a file.

                                    CODE REQUEST:
                                    {prompt}
                                    """;

        return await llamaService.GetLlamaResponseAsync(codeGenerationPrompt, cancellationToken);
    }

    public static async Task<string> GenerateFileContentAsync(
        this ILlamaService llamaService,
        string prompt,
        string fileType = "code",
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(llamaService);
        ArgumentNullException.ThrowIfNull(prompt);

        var instructionsBasedOnType = fileType.ToLower() switch
        {
            "code" => """
                      1. Generate ONLY the requested code without adding ANY additional explanation.
                      2. Include all necessary imports/includes at the beginning of the file.
                      3. The code must be complete, functional, and follow best practices for the language in question.
                      """,

            "documentation" => """
                               1. Generate clean, well-structured documentation content.
                               2. Use appropriate formatting and headings.
                               3. Be comprehensive yet concise.
                               """,

            "configuration" => """
                               1. Generate valid configuration content in the appropriate format (JSON, YAML, XML, etc.)
                               2. Include necessary comments where appropriate for the format.
                               3. Use proper indentation and structure for the configuration format.
                               """,

            _ => """
                 1. Generate clean, properly formatted content appropriate for the requested file type.
                 2. Follow best practices for the specific file format.
                 3. Ensure the content is complete and ready to use.
                 """
        };

        var fileGenerationPrompt = $"""
                                    IMPORTANT INSTRUCTIONS - STRICT COMPLIANCE REQUIRED:

                                    {instructionsBasedOnType}

                                    GENERAL REQUIREMENTS:
                                    1. DO NOT include code block markers like ``` or any other text beyond the actual file content.
                                    2. DO NOT add introductory or explanatory comments before or after the content.
                                    3. Your response should contain ONLY the file content, as if you were writing directly to a file.

                                    FILE CONTENT REQUEST:
                                    {prompt}
                                    """;

        return await llamaService.GetLlamaResponseAsync(fileGenerationPrompt, cancellationToken);
    }

    private static string CleanAndValidateJsonResponse(string response, bool expectArray)
    {
        response = response.Trim();
        response = Regex.Replace(response, @"```(json)?|`", string.Empty).Trim();

        var firstBrace = response.IndexOfAny(['{', '[']);
        if (firstBrace < 0)
        {
            throw new InvalidOperationException("No JSON structure found in response");
        }

        int lastBrace = -1;
        int depth = 0;
        for (int i = firstBrace; i < response.Length; i++)
        {
            if (response[i] == '{' || response[i] == '[')
            {
                depth++;
            }
            else if (response[i] == '}' || response[i] == ']')
            {
                depth--;
                if (depth == 0)
                {
                    lastBrace = i;
                    break;
                }
            }
        }

        if (lastBrace < 0)
        {
            throw new InvalidOperationException("Unclosed JSON structure in response");
        }

        response = response.Substring(firstBrace, lastBrace - firstBrace + 1);
        response = Regex.Replace(response, @",(\s*[}\]])", "$1");

        try
        {
            using var doc = JsonDocument.Parse(response, new JsonDocumentOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Skip
            });

            return expectArray switch
            {
                true when doc.RootElement.ValueKind != JsonValueKind.Array => throw new InvalidOperationException(
                    $"Expected JSON array but got {doc.RootElement.ValueKind}"),
                false when doc.RootElement.ValueKind != JsonValueKind.Object => throw new InvalidOperationException(
                    $"Expected JSON object but got {doc.RootElement.ValueKind}"),
                _ => response
            };
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Invalid JSON format: {ex.Message}. Response: {response}");
        }
    }

    private static string BuildJsonExample(Type type, HashSet<Type>? processedTypes = null)
    {
        processedTypes ??= [];

        if (!processedTypes.Add(type))
            return "{}";

        try
        {
            if (IsDictionaryType(type))
            {
                var keyType = type.GetGenericArguments()[0];
                var valueType = type.GetGenericArguments()[1];
                return $"{{ \"{GetDictionaryKeyExample(keyType)}\": {BuildJsonExample(valueType, processedTypes)} }}";
            }

            if (IsCollectionType(type))
            {
                var elementType = GetCollectionElementType(type);
                return $"[{BuildJsonExample(elementType, processedTypes)}]";
            }

            if (!type.IsClass || type == typeof(string)) return GetPrimitiveExampleValue(type);
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetCustomAttribute<JsonIgnoreAttribute>() == null)
                .OrderBy(p => p.GetCustomAttribute<JsonPropertyOrderAttribute>()?.Order ?? 0);

            var jsonProperties = properties.Select(prop =>
            {
                var propType = prop.PropertyType;
                var exampleValue = GetPropertyExampleValue(propType, processedTypes);
                return $"\"{GetPropertyName(prop)}\": {exampleValue}";
            });

            return $"{{{string.Join(", ", jsonProperties)}}}";
        }
        finally
        {
            processedTypes.Remove(type);
        }
    }

    private static string GetPropertyExampleValue(Type propType, HashSet<Type> processedTypes)
    {
        if (propType == typeof(List<string>))
            return "[\"example1\", \"example2\"]";

        if (propType == typeof(Dictionary<string, string>))
            return "{ \"key1\": \"value1\", \"key2\": \"value2\" }";

        if (IsDictionaryType(propType))
        {
            var valueType = propType.GetGenericArguments()[1];
            return $"{{ \"exampleKey\": {BuildJsonExample(valueType, processedTypes)} }}";
        }

        if (IsCollectionType(propType))
        {
            var elementType = GetCollectionElementType(propType);
            return $"[{BuildJsonExample(elementType, processedTypes)}]";
        }

        return BuildJsonExample(propType, processedTypes);
    }

    private static bool IsDictionaryType(Type type)
    {
        if (!type.IsGenericType)
            return false;

        var genericType = type.GetGenericTypeDefinition();
        return genericType == typeof(Dictionary<,>) ||
               genericType == typeof(IDictionary<,>);
    }

    private static string GetDictionaryKeyExample(Type keyType)
    {
        return keyType == typeof(string) ? "exampleKey" :
            keyType == typeof(int) ? "42" :
            keyType == typeof(Guid) ? "00000000-0000-0000-0000-000000000000" :
            "key";
    }

    private static string GetPrimitiveExampleValue(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        return underlyingType switch
        {
            _ when underlyingType == typeof(string) => "\"example\"",
            _ when underlyingType == typeof(int) => "42",
            _ when underlyingType == typeof(bool) => "true",
            _ when underlyingType == typeof(double) => "3.14",
            _ when underlyingType == typeof(DateTime) => "\"2023-01-01T00:00:00\"",
            _ when underlyingType == typeof(Guid) => "\"00000000-0000-0000-0000-000000000000\"",
            _ when underlyingType.IsEnum => $"\"{Enum.GetNames(underlyingType).FirstOrDefault()}\"",
            _ => "\"value\""
        };
    }

    private static string GetPropertyName(PropertyInfo prop)
    {
        return prop.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ??
               JsonNamingPolicy.CamelCase.ConvertName(prop.Name);
    }

    private static bool IsCollectionType(Type type)
    {
        if (type.IsArray) return true;
        if (!type.IsGenericType) return false;

        var genericType = type.GetGenericTypeDefinition();
        return genericType == typeof(List<>) ||
               genericType == typeof(IEnumerable<>) ||
               genericType == typeof(ICollection<>) ||
               genericType == typeof(IList<>);
    }

    private static Type GetCollectionElementType(Type type)
    {
        return type.IsArray ? type.GetElementType()! : type.GetGenericArguments()[0];
    }
}