using Google.GenAI;
using Google.GenAI.Types;
using StationAI.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Reflection;
using Type = Google.GenAI.Types.Type;

namespace StationAI.Adapters.Outbound
{
    public class GeminiAdapter : ILargeLanguageModelService
    {
        private readonly Client _client = new();
        private readonly string _modelName = "gemini-3.1-flash-lite";
        private readonly string _backupModelName = "gemini-2.5-flash-lite";

        private static readonly HttpStatusCode[] TransientStatusCodes =
        [
            HttpStatusCode.TooManyRequests,
            HttpStatusCode.InternalServerError,
            HttpStatusCode.BadGateway,
            HttpStatusCode.ServiceUnavailable,
            HttpStatusCode.GatewayTimeout
        ];

        public async Task<string> SendPrompt(string prompt, System.Type targetSchemaType)
        {
            var config = new GenerateContentConfig
            {
                ResponseMimeType = "application/json",
                ResponseSchema = BuildSchema(targetSchemaType)
            };

            try
            {
                var response = await _client.Models.GenerateContentAsync(_modelName, prompt, config);
                return response.Text ?? string.Empty;
            }
            catch (Exception ex) when (IsTransientFailure(ex))
            {
                var response = await _client.Models.GenerateContentAsync(_backupModelName, prompt, config);
                return response.Text ?? string.Empty;
            }
        }

        private static bool IsTransientFailure(Exception ex) =>
            ex switch
            {
                HttpRequestException http when http.StatusCode.HasValue => TransientStatusCodes.Contains(http.StatusCode.Value),
                TaskCanceledException => true,
                TimeoutException => true,
                _ => false
            };

        private static Schema BuildSchema(System.Type type)
        {
            if (IsObjectList(type, out var elementType))
            {
                return new Schema
                {
                    Type = Type.Array,
                    Items = BuildObjectSchema(elementType)
                };
            }

            return BuildObjectSchema(type);
        }

        private static Schema BuildObjectSchema(System.Type type)
        {
            var properties = new Dictionary<string, Schema>();
            var requiredFields = new List<string>();

            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                Type propType;
                if (prop.PropertyType == typeof(string))
                    propType = Type.String;
                else if (prop.PropertyType == typeof(bool))
                    propType = Type.Boolean;
                else
                    propType = Type.Integer;

                var schema = new Schema { Type = propType };

                var rangeAttr = prop.GetCustomAttribute<RangeAttribute>();
                if (rangeAttr is not null && propType == Type.Integer)
                {
                    schema.Minimum = Convert.ToDouble(rangeAttr.Minimum);
                    schema.Maximum = Convert.ToDouble(rangeAttr.Maximum);
                }

                properties.Add(prop.Name, schema);
                requiredFields.Add(prop.Name);
            }

            return new Schema
            {
                Type = Type.Object,
                Properties = properties,
                Required = requiredFields
            };
        }

        private static bool IsObjectList(System.Type type, out System.Type elementType)
        {
            elementType = typeof(object);

            if (type.IsGenericType)
            {
                var def = type.GetGenericTypeDefinition();
                if (def == typeof(List<>) || def == typeof(IEnumerable<>) || def == typeof(IReadOnlyList<>))
                {
                    elementType = type.GetGenericArguments()[0];
                    return true;
                }
            }

            return false;
        }
    }
}