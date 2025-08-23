using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace HealthCheckApi.Services
{
    public record ChatReply(string Reply, string Source, string? Error = null);

  
    public class ChatService
    {
        private readonly HttpClient _http;
        private readonly string _apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "";
        private readonly string _model  = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "gpt-4o-mini";

        public ChatService(HttpClient http) { _http = http; }

        public async Task<ChatReply> AskAsync(string userMessage, string systemPrompt)
{
    if (string.IsNullOrWhiteSpace(_apiKey))
        return new ChatReply(
            Reply: LocalFallback(userMessage),
            Source: "fallback",
            Error: "AI not configured."
        );

    try
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
        req.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
        req.Content = JsonContent.Create(new
        {
            model = _model,
            messages = new object[] {
                new { role = "system", content = systemPrompt },
                new { role = "user",   content = userMessage }
            },
            temperature = 0.2
        });

        var resp = await _http.SendAsync(req);
        var text = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
        {
            var friendly = TryExtractOpenAIError(resp.StatusCode, text);
            return new ChatReply(
                Reply: LocalFallback(userMessage),
                Source: "fallback",
                Error: friendly
            );
        }

        var ok = JsonDocument.Parse(text);
        var content = ok.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return new ChatReply(
            Reply: content ?? LocalFallback(userMessage),
            Source: content != null ? "ai" : "fallback",
            Error: content != null ? null : "Empty AI content; used fallback."
        );
    }
    catch (Exception ex)
    {
        return new ChatReply(
            Reply: LocalFallback(userMessage),
            Source: "fallback",
            Error: $"AI call failed: {ex.Message}"
        );
    }
}
  private static string TryExtractOpenAIError(System.Net.HttpStatusCode code, string body)
{
    try
    { 
        var j = JsonDocument.Parse(body);
        var msg = j.RootElement.GetProperty("error").GetProperty("message").GetString();
        return $"AI error ({(int)code}): {msg}";
    }
    catch
    {
        return $"AI error ({(int)code}): {body}";
    }
}

        // Minimal rule-based answers 
        private static string LocalFallback(string msg)
        {
            var m = msg?.Trim().ToLowerInvariant() ?? "";

            // health
            if (m.Contains("health"))
                return "Check health endpoints:\n• GET /health (basic)\n• GET /health/ready (readiness)\n• GET /health/live (liveness)";

            // stock of {name}
            var stock = Regex.Match(m, @"stock of ([\w\s\-]+)");
            if (stock.Success)
            {
                var name = stock.Groups[1].Value.Trim();
                return $"To check stock for '{name}': GET /items?search={name}. In the UI, use the search box.";
            }

            // deduct {n} from {name}
            var deduct = Regex.Match(m, @"deduct (\d+) from ([\w\s\-]+)");
            if (deduct.Success)
            {
                var amount = deduct.Groups[1].Value;
                var name = deduct.Groups[2].Value.Trim();
                return $"To deduct {amount} from '{name}': call POST /items/{{id}}/deduct with body {{ amount: {amount}, reason: \"...\", user: \"...\" }}.";
            }

            // auth
            if (m.Contains("login") || m.Contains("auth") || m.Contains("token"))
                return "Auth flow:\n• POST /auth/register → returns token\n• POST /auth/login → returns token\n• Use Authorization: Bearer <token>\n• GET /auth/me to verify.";

            // generic
            return "I can help with inventory endpoints. Try:\n• “health”\n• “stock of gauze pads”\n• “deduct 2 from syringes because ER”.";
        }
    }
}
