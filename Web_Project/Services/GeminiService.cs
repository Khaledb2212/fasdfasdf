using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Web_Project.Services
{
    public class GeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        // Centralize model names so you don't hunt strings everywhere
        private const string TextModelAction = "gemini-2.5-flash:generateContent";
        private const string ImageModelAction = "gemini-2.5-flash-image:generateContent";

        public GeminiService(IConfiguration config, HttpClient httpClient)
        {
            _httpClient = httpClient;
            _apiKey = config["GoogleAI:ApiKey"] ?? "";
        }

        // -------------------------
        // TEXT: Plan from Photo
        // -------------------------
        public async Task<string> AnalyzeImage(byte[] imageBytes, string mimeType)
        {
            var base64Image = Convert.ToBase64String(imageBytes);

            var prompt =
                "Analyze this person's body type visually. Provide a strict but sustainable diet and exercise plan. " +
                "Also write a short vivid paragraph describing how this person will look very changed after 50 months of following the plan.and if it was fat make it very fit and if it was thin make it a big bodybuilde";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new { text = prompt },
                            new
                            {
                                inline_data = new
                                {
                                    mime_type = mimeType,
                                    data = base64Image
                                }
                            }
                        }
                    }
                }
            };

            return await SendTextRequest(TextModelAction, requestBody);
        }

        // -------------------------
        // TEXT: Plan from Stats
        // -------------------------
        public async Task<string> AnalyzeText(double height, double weight, int age)
        {
            var prompt =
                $"Act as an expert fitness coach. My stats are Height: {height}cm, Weight: {weight}kg, Age: {age}. " +
                "Provide a detailed diet and exercise plan. Also write a short vivid paragraph describing how I will look after 50 months. and if it was fat make it very fit and if it was thin make it a big bodybuilde";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new object[] { new { text = prompt } }
                    }
                }
            };

            return await SendTextRequest(TextModelAction, requestBody);
        }

        // -------------------------
        // IMAGE EDITING: Future self from uploaded photo (Nano Banana)
        // -------------------------
        public async Task<byte[]?> GenerateFutureSelfPreviewFromPhoto(byte[] imageBytes, string mimeType)
        {
            var base64Image = Convert.ToBase64String(imageBytes);

            var prompt =
                  "Generate an EDITED version of the provided photo.  (same face identity). " +
                  "Make the body look super athletic as and if it was fat make it very fit and if it was thin make it a big bodybuilde. " +
                  "Keep clothing and background similar. and make big big changes Photorealistic. " +
                  "Return ONLY the edited image (no text).";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new { text = prompt },
                            new
                            {
                                inline_data = new
                                {
                                    mime_type = mimeType,
                                    data = base64Image
                                }
                            }
                        }
                    }
                },
                generationConfig = new
                {
                    // Docs show "Text"/"Image" casing (REST/SDK examples)
                    responseModalities = new[] { "Image" }
                }
            };

            return await SendImageRequest(ImageModelAction, requestBody);
        }

        // -------------------------
        // IMAGE GENERATION: Future self from stats (no identity guarantee)
        // -------------------------
        public async Task<byte[]?> GenerateFutureSelfPreviewFromStats(double height, double weight, int age)
        {
            var prompt =
                $"Create a photorealistic full-body portrait of a person who is {height}cm, {weight}kg, age {age}, " +
                "then show how they might look look super athletic. and if it was fat make it very fit and if it was thin make it a big bodybuilder" +
                "fully clothed, and make big big changes. Photorealistic.";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new object[] { new { text = prompt } }
                    }
                },
                generationConfig = new
                {
                    responseModalities = new[] { "Image" }
                }
            };

            return await SendImageRequest(ImageModelAction, requestBody);
        }

        // -------------------------
        // Low-level helpers
        // -------------------------
        private async Task<string> SendTextRequest(string modelAction, object requestBody)
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelAction}";

            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Add("x-goog-api-key", _apiKey);
            req.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var resp = await _httpClient.SendAsync(req);
            var json = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                return $"Error from AI ({(int)resp.StatusCode}): {json}";

            using var doc = JsonDocument.Parse(json);

            try
            {
                return doc.RootElement.GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString() ?? "No result.";
            }
            catch
            {
                return "Could not parse AI response.";
            }
        }

        private async Task<byte[]?> SendImageRequest(string modelAction, object requestBody)
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelAction}";

            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Add("x-goog-api-key", _apiKey);
            req.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var resp = await _httpClient.SendAsync(req);
            var json = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                throw new Exception($"Image API error {(int)resp.StatusCode}: {json}");


            using var doc = JsonDocument.Parse(json);

            var parts = doc.RootElement.GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts");

            foreach (var part in parts.EnumerateArray())
            {
                // Some responses use inlineData (camelCase)
                if (part.TryGetProperty("inlineData", out var inlineData) &&
                    inlineData.TryGetProperty("data", out var dataEl1))
                {
                    var b64 = dataEl1.GetString();
                    if (!string.IsNullOrWhiteSpace(b64))
                        return Convert.FromBase64String(b64);
                }

                // Some responses use inline_data (snake_case)
                if (part.TryGetProperty("inline_data", out var inline_data) &&
                    inline_data.TryGetProperty("data", out var dataEl2))
                {
                    var b64 = dataEl2.GetString();
                    if (!string.IsNullOrWhiteSpace(b64))
                        return Convert.FromBase64String(b64);
                }
            }

            return null;
        }
    }
}


