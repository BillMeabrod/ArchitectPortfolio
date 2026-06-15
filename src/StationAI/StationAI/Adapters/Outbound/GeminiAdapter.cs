using Google.GenAI;
using Google.GenAI.Types;
using StationAI.Core.Interfaces;
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

        public async Task<string> SendPrompt(string prompt, System.Type targetSchemaType)
        {
            var config = new GenerateContentConfig
            {
                ResponseMimeType = "application/json",
                ResponseSchema = GenerateSchemaFromType(targetSchemaType)
            };

            try
            {
                var response = await _client.Models.GenerateContentAsync(_modelName, prompt, config);
                return response.Text ?? string.Empty;
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
            {
                var response = await _client.Models.GenerateContentAsync(_backupModelName, prompt, config);
                return response.Text ?? string.Empty;
            }
        }

        private Schema GenerateSchemaFromType(System.Type type)
        {
            var properties = new Dictionary<string, Schema>();
            var requiredFields = new List<string>();

            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var propType = prop.PropertyType == typeof(string) ? Type.String : Type.Integer;

                properties.Add(prop.Name, new Schema { Type = propType });
                requiredFields.Add(prop.Name);
            }

            return new Schema
            {
                Type = Type.Object,
                Properties = properties,
                Required = requiredFields
            };
        }
    }
}


