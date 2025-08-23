using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using HealthCheckApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace HealthCheckApi.Services
{
    // Records and DTOs
    public record ChatReply(string Reply, string Source, string? Error = null, double? Confidence = null);
    
    public record ChatMessage(string Role, string Content, DateTime Timestamp, Dictionary<string, object>? Meta = null);
    
    public record ChatRequest(string Message);
    
    public record IntentResult(ChatIntent Intent, Dictionary<string, object> Entities, double Confidence);
    
    public record ProactiveAlert(string Message, string Type, DateTime Timestamp, string Action);

    // Enums
    public enum ChatIntent
    {
        CheckStock, DeductItems, AddStock, LowStockAlert, 
        HealthCheck, Auth, GeneralHelp, BulkOperations,
        UsageReport, UserManagement, SystemStatus
    }

    // Session Management Interface
    public interface IChatSessionService
    {
        Task<List<ChatMessage>> GetHistoryAsync(string userId);
        Task AddMessageAsync(string userId, ChatMessage message);
        Task<Dictionary<string, object>> GetSessionDataAsync(string userId);
        Task SetSessionDataAsync(string userId, string key, object value);
        Task ClearHistoryAsync(string userId);
    }

    // Session Management Implementation
    public class ChatSessionService : IChatSessionService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<ChatSessionService> _logger;

        public ChatSessionService(IMemoryCache cache, ILogger<ChatSessionService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public async Task<List<ChatMessage>> GetHistoryAsync(string userId)
        {
            var key = $"chat_history_{userId}";
            var history = _cache.Get<List<ChatMessage>>(key) ?? new List<ChatMessage>();
            _logger.LogDebug("Retrieved {Count} messages for user {UserId}", history.Count, userId);
            return await Task.FromResult(history);
        }

        public async Task AddMessageAsync(string userId, ChatMessage message)
        {
            var key = $"chat_history_{userId}";
            var history = await GetHistoryAsync(userId);
            history.Add(message);

            // Keep only last 10 messages to prevent memory bloat
            if (history.Count > 10)
            {
                history.RemoveRange(0, history.Count - 10);
            }

            _cache.Set(key, history, TimeSpan.FromHours(2));
            _logger.LogDebug("Added message for user {UserId}, total messages: {Count}", userId, history.Count);
        }

        public async Task<Dictionary<string, object>> GetSessionDataAsync(string userId)
        {
            var key = $"chat_session_{userId}";
            var sessionData = _cache.Get<Dictionary<string, object>>(key) ?? new Dictionary<string, object>();
            return await Task.FromResult(sessionData);
        }

        public async Task SetSessionDataAsync(string userId, string key, object value)
        {
            var sessionKey = $"chat_session_{userId}";
            var sessionData = await GetSessionDataAsync(userId);
            sessionData[key] = value;
            _cache.Set(sessionKey, sessionData, TimeSpan.FromHours(2));
            _logger.LogDebug("Set session data {Key} for user {UserId}", key, userId);
        }

        public async Task ClearHistoryAsync(string userId)
        {
            var historyKey = $"chat_history_{userId}";
            var sessionKey = $"chat_session_{userId}";
            _cache.Remove(historyKey);
            _cache.Remove(sessionKey);
            _logger.LogDebug("Cleared chat data for user {UserId}", userId);
            await Task.CompletedTask;
        }
    }

    // Intent Classification Service
    public class IntentClassifier
    {
        private static readonly Dictionary<string, (ChatIntent Intent, string[] Keywords)> IntentPatterns = new()
        {
            ["stock"] = (ChatIntent.CheckStock, new[] { "stock", "inventory", "quantity", "available", "how many", "how much", "level", "count" }),
            ["deduct"] = (ChatIntent.DeductItems, new[] { "deduct", "use", "take", "remove", "subtract", "dispense", "consume" }),
            ["add"] = (ChatIntent.AddStock, new[] { "add", "restock", "replenish", "increase", "receive", "new shipment" }),
            ["low_stock"] = (ChatIntent.LowStockAlert, new[] { "low stock", "running low", "shortage", "reorder", "almost empty" }),
            ["health"] = (ChatIntent.HealthCheck, new[] { "health", "status", "alive", "ready", "ping", "system check" }),
            ["auth"] = (ChatIntent.Auth, new[] { "login", "register", "token", "authentication", "password", "user", "sign in" }),
            ["usage"] = (ChatIntent.UsageReport, new[] { "usage", "history", "consumed", "report", "analytics", "trends" }),
            ["bulk"] = (ChatIntent.BulkOperations, new[] { "bulk", "multiple", "batch", "several", "many items" }),
            ["user_mgmt"] = (ChatIntent.UserManagement, new[] { "users", "permissions", "roles", "access", "admin" })
        };

        public static IntentResult ClassifyIntent(string message)
        {
            var entities = new Dictionary<string, object>();
            var lowerMessage = message.ToLowerInvariant();
            var bestIntent = ChatIntent.GeneralHelp;
            var bestScore = 0.0;

            foreach (var (key, (intent, keywords)) in IntentPatterns)
            {
                var score = keywords.Count(keyword => lowerMessage.Contains(keyword)) / (double)keywords.Length;
                if (score > bestScore)
                {
                    bestScore = score;
                    bestIntent = intent;
                }
            }

            // Extract specific entities based on intent
            switch (bestIntent)
            {
                case ChatIntent.CheckStock:
                    var itemMatch = Regex.Match(lowerMessage, @"(?:stock of|how many|how much|check)\s+([a-zA-Z\s\-]+?)(?:\s|$|[?.,!])");
                    if (itemMatch.Success)
                    {
                        entities["item_name"] = itemMatch.Groups[1].Value.Trim();
                        bestScore = Math.Max(bestScore, 0.8);
                    }
                    break;

                case ChatIntent.DeductItems:
                    var quantityMatch = Regex.Match(lowerMessage, @"(?:deduct|use|take|remove)\s+(\d+)");
                    var itemDeductMatch = Regex.Match(lowerMessage, @"(\d+)\s+([a-zA-Z\s\-]+?)(?:\s|$|[?.,!])");
                    
                    if (quantityMatch.Success)
                    {
                        entities["quantity"] = int.Parse(quantityMatch.Groups[1].Value);
                        bestScore = Math.Max(bestScore, 0.8);
                    }
                    if (itemDeductMatch.Success)
                    {
                        entities["quantity"] = int.Parse(itemDeductMatch.Groups[1].Value);
                        entities["item_name"] = itemDeductMatch.Groups[2].Value.Trim();
                        bestScore = 0.9;
                    }
                    
                    var reasonMatch = Regex.Match(lowerMessage, @"(?:because|for|reason)\s+([^.!?]+)");
                    if (reasonMatch.Success)
                    {
                        entities["reason"] = reasonMatch.Groups[1].Value.Trim();
                    }
                    break;

                case ChatIntent.AddStock:
                    var addQuantityMatch = Regex.Match(lowerMessage, @"(?:add|receive|restock)\s+(\d+)");
                    if (addQuantityMatch.Success)
                    {
                        entities["quantity"] = int.Parse(addQuantityMatch.Groups[1].Value);
                        bestScore = Math.Max(bestScore, 0.8);
                    }
                    break;
            }

            return new IntentResult(bestIntent, entities, Math.Max(bestScore, 0.5));
        }
    }

    // Main Chat Service
    public class ChatService
    {
        public record ChatContext(
            string UserId,
            List<string> Roles,
            List<ChatMessage> History,
            Dictionary<string, object> SessionData
        );

        private readonly HttpClient _http;
        private readonly ILogger<ChatService> _logger;
        private readonly IChatSessionService _sessionService;
        private readonly string _apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "";
        private readonly string _model = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "gpt-4o-mini";

        public ChatService(HttpClient http, ILogger<ChatService> logger, IChatSessionService sessionService)
        {
            _http = http;
            _logger = logger;
            _sessionService = sessionService;
        }

        public async Task<ChatReply> AskAsync(string userMessage, string systemPrompt, ChatContext? context = null)
        {
            var enhancedPrompt = BuildContextualPrompt(systemPrompt, context);
            
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                _logger.LogWarning("OpenAI API key not configured, using fallback");
                return new ChatReply(
                    Reply: LocalFallback(userMessage),
                    Source: "fallback",
                    Error: "AI not configured."
                );
            }

            try
            {
                // Build messages array including conversation history
                var messages = new List<object>
                {
                    new { role = "system", content = enhancedPrompt }
                };

                // Add recent conversation history
                if (context?.History != null && context.History.Any())
                {
                    var recentHistory = context.History.TakeLast(4); // Last 4 messages for context
                    foreach (var msg in recentHistory)
                    {
                        messages.Add(new { role = msg.Role, content = msg.Content });
                    }
                }

                messages.Add(new { role = "user", content = userMessage });

                using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
                req.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
                
                req.Content = JsonContent.Create(new
                {
                    model = _model,
                    messages = messages.ToArray(),
                    temperature = 0.2,
                    max_tokens = 500
                });

                _logger.LogDebug("Sending request to OpenAI for user {UserId}", context?.UserId ?? "anonymous");

                var resp = await _http.SendAsync(req);
                var text = await resp.Content.ReadAsStringAsync();

                if (!resp.IsSuccessStatusCode)
                {
                    var friendly = TryExtractOpenAIError(resp.StatusCode, text);
                    _logger.LogError("OpenAI API error: {Error}", friendly);
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

                _logger.LogInformation("Successfully received AI response for user {UserId}", context?.UserId ?? "anonymous");

                return new ChatReply(
                    Reply: content ?? LocalFallback(userMessage),
                    Source: content != null ? "ai" : "fallback",
                    Error: content != null ? null : "Empty AI content; used fallback.",
                    Confidence: 0.85
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI call failed for user {UserId}: {Message}", context?.UserId ?? "unknown", userMessage);
                return new ChatReply(
                    Reply: LocalFallback(userMessage),
                    Source: "fallback",
                    Error: $"AI call failed: {ex.Message}"
                );
            }
        }

        public async Task<ChatReply> AskWithDataAsync(string userMessage, string systemPrompt, ItemsDbContext db, ChatContext context)
        {
            try
            {
                // Classify intent first
                var intentResult = IntentClassifier.ClassifyIntent(userMessage);
                _logger.LogDebug("Classified intent: {Intent} with confidence {Confidence}", 
                    intentResult.Intent, intentResult.Confidence);

                // Detect if query needs live data and get relevant data
                if (IsInventoryQuery(userMessage) || intentResult.Intent == ChatIntent.CheckStock)
                {
                    var relevantData = await GetRelevantInventoryData(userMessage, db, intentResult);
                    systemPrompt += $"\n\nCurrent inventory data: {JsonSerializer.Serialize(relevantData)}";
                    systemPrompt += "\nUse this real data in your response. Be specific with quantities and item names.";
                }

                // Handle low stock alerts
                if (intentResult.Intent == ChatIntent.LowStockAlert)
                {
                    var alerts = await GetProactiveAlerts(context, db);
                    if (alerts.Any())
                    {
                        systemPrompt += $"\n\nCurrent alerts: {JsonSerializer.Serialize(alerts)}";
                    }
                }

                // Store search context for follow-up questions
                if (intentResult.Entities.ContainsKey("item_name"))
                {
                    await _sessionService.SetSessionDataAsync(context.UserId, "last_search", 
                        intentResult.Entities["item_name"]);
                }

                // Enhanced with real data
                var reply = await AskAsync(userMessage, systemPrompt, context);
                
                // Store the conversation
                await _sessionService.AddMessageAsync(context.UserId, 
                    new ChatMessage("user", userMessage, DateTime.UtcNow));
                await _sessionService.AddMessageAsync(context.UserId, 
                    new ChatMessage("assistant", reply.Reply, DateTime.UtcNow, 
                        new Dictionary<string, object> { ["intent"] = intentResult.Intent.ToString() }));

                return reply;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AskWithDataAsync for user {UserId}", context.UserId);
                return new ChatReply(
                    Reply: LocalFallback(userMessage),
                    Source: "fallback",
                    Error: $"Data integration failed: {ex.Message}"
                );
            }
        }

        public async Task<List<ProactiveAlert>> GetProactiveAlerts(ChatContext context, ItemsDbContext db)
        {
            var alerts = new List<ProactiveAlert>();

            try
            {
                // Low stock items
                var lowStock = await db.Items
                    .Where(i => i.ReorderLevel.HasValue && i.Quantity <= i.ReorderLevel.Value)
                    .CountAsync();

                if (lowStock > 0)
                {
                    alerts.Add(new ProactiveAlert(
                        $"⚠️ {lowStock} item(s) below reorder level",
                        "warning",
                        DateTime.UtcNow,
                        "show_low_stock"
                    ));
                }

                // Out of stock items
                var outOfStock = await db.Items.CountAsync(i => i.Quantity == 0);
                if (outOfStock > 0)
                {
                    alerts.Add(new ProactiveAlert(
                        $"🚨 {outOfStock} item(s) out of stock",
                        "critical",
                        DateTime.UtcNow,
                        "show_out_of_stock"
                    ));
                }

                // High usage today 
                if (db.UsageRecords != null)
                {
                    var highUsageToday = await db.UsageRecords
                        .Where(u => u.Timestamp.Date == DateTime.UtcNow.Date)
                        .GroupBy(u => u.ItemId)
                        .Where(g => g.Sum(x => x.Amount) > 20)
                        .CountAsync();

                    if (highUsageToday > 0)
                    {
                        alerts.Add(new ProactiveAlert(
                            $"📊 High usage detected on {highUsageToday} item(s) today",
                            "info",
                            DateTime.UtcNow,
                            "usage_report"
                        ));
                    }
                }

                _logger.LogDebug("Generated {Count} proactive alerts for user {UserId}", alerts.Count, context.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating proactive alerts");
            }

            return alerts;
        }

        private string BuildContextualPrompt(string basePrompt, ChatContext? context)
        {
            if (context == null) return basePrompt;

            var prompt = basePrompt;

            // Add user role context
            if (context.Roles.Contains("Admin"))
            {
                prompt += "\nUser is Admin - can delete items, manage users, access all endpoints. Provide advanced options.";
            }
            else if (context.Roles.Any(r => new[] { "Physician", "Clerical" }.Contains(r)))
            {
                prompt += "\nUser has view-only access - cannot modify inventory. Focus on read operations and reporting.";
            }
            else if (context.Roles.Any(r => new[] { "Pharmacist", "SupplyChain", "Nurse" }.Contains(r)))
            {
                prompt += "\nUser can modify inventory - can add, update, deduct items. Show both read and write operations.";
            }

            // Add conversation context
            if (context.History.Any())
            {
                prompt += "\n\nRecent conversation context:";
                var recentHistory = context.History.TakeLast(2);
                foreach (var msg in recentHistory)
                {
                    var truncated = msg.Content.Length > 100 ? msg.Content.Substring(0, 100) + "..." : msg.Content;
                    prompt += $"\n- {msg.Role}: {truncated}";
                }
            }

            // Add session data context
            if (context.SessionData.Any())
            {
                if (context.SessionData.ContainsKey("last_search"))
                {
                    prompt += $"\nUser previously searched for: {context.SessionData["last_search"]}";
                }
                if (context.SessionData.ContainsKey("current_item"))
                {
                    prompt += $"\nUser is currently viewing item: {context.SessionData["current_item"]}";
                }
            }

            return prompt;
        }

        private static bool IsInventoryQuery(string message)
        {
            var inventoryKeywords = new[] {
                "stock", "inventory", "quantity", "available", "count",
                "how many", "how much", "level", "amount", "items",
                "supplies", "equipment", "materials", "low stock",
                "out of stock", "reorder", "shortage", "check", "show"
            };

            var lowerMessage = message.ToLowerInvariant();
            return inventoryKeywords.Any(keyword => lowerMessage.Contains(keyword));
        }

        private async Task<object> GetRelevantInventoryData(string userMessage, ItemsDbContext db, IntentResult intentResult)
        {
            // Use extracted entities from intent classification
            if (intentResult.Entities.ContainsKey("item_name"))
            {
                var itemName = intentResult.Entities["item_name"].ToString();
                var items = await db.Items
                    .Where(i => EF.Functions.ILike(i.Name, $"%{itemName}%"))
                    .Select(i => new { 
                        i.Id, 
                        i.Name, 
                        i.Quantity, 
                        i.ReorderLevel,
                        LowStock = i.ReorderLevel.HasValue && i.Quantity <= i.ReorderLevel.Value
                    })
                    .ToListAsync();

                _logger.LogDebug("Found {Count} items matching '{ItemName}'", items.Count, itemName);
                return new { type = "specific_items", query = itemName, data = items };
            }

            // asking about low stock
            if (userMessage.ToLowerInvariant().Contains("low stock") || intentResult.Intent == ChatIntent.LowStockAlert)
            {
                var lowStock = await db.Items
                    .Where(i => i.ReorderLevel.HasValue && i.Quantity <= i.ReorderLevel.Value)
                    .Select(i => new { 
                        i.Id, 
                        i.Name, 
                        i.Quantity, 
                        i.ReorderLevel,
                        Deficit = i.ReorderLevel.Value - i.Quantity
                    })
                    .OrderBy(i => i.Quantity)
                    .ToListAsync();

                _logger.LogDebug("Found {Count} low stock items", lowStock.Count);
                return new { type = "low_stock", data = lowStock };
            }

            // General inventory summary
            var summary = new
            {
                total_items = await db.Items.CountAsync(),
                low_stock_count = await db.Items.CountAsync(i => i.ReorderLevel.HasValue && i.Quantity <= i.ReorderLevel.Value),
                out_of_stock_count = await db.Items.CountAsync(i => i.Quantity == 0),
                total_quantity = await db.Items.SumAsync(i => i.Quantity)
            };

            _logger.LogDebug("Generated inventory summary");
            return new { type = "summary", data = summary };
        }

        private static List<string> ExtractItemNames(string message)
        {
            var patterns = new[] {
                @"stock of ([a-zA-Z\s\-]+?)(?:\s|$|[?.,!])",
                @"how many ([a-zA-Z\s\-]+?)(?:\s|$|[?.,!])",
                @"([a-zA-Z\s\-]+?) available",
                @"check ([a-zA-Z\s\-]+?)(?:\s|$|[?.,!])"
            };

            var items = new HashSet<string>();
            foreach (var pattern in patterns)
            {
                var matches = Regex.Matches(message, pattern, RegexOptions.IgnoreCase);
                foreach (Match match in matches)
                {
                    if (match.Groups.Count > 1)
                    {
                        var item = match.Groups[1].Value.Trim();
                        if (item.Length > 2 && item.Length < 50) // Basic validation
                        {
                            items.Add(item);
                        }
                    }
                }
            }

            return items.ToList();
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

        // Enhanced rule-based fallback with more patterns
        private static string LocalFallback(string msg)
        {
            var m = msg?.Trim().ToLowerInvariant() ?? "";

            // Health checks
            if (m.Contains("health") || m.Contains("status") || m.Contains("ping"))
                return "🏥 **Health Check Endpoints:**\n• `GET /health` - Basic health check\n• `GET /health/ready` - Readiness probe\n• `GET /health/live` - Liveness probe\n\nAll should return status information about the API.";

            // Stock checking with item extraction
            var stockPatterns = new[] {
                @"stock of ([a-zA-Z\s\-]+)",
                @"how many ([a-zA-Z\s\-]+)",
                @"check ([a-zA-Z\s\-]+)"
            };

            foreach (var pattern in stockPatterns)
            {
                var match = Regex.Match(m, pattern);
                if (match.Success)
                {
                    var name = match.Groups[1].Value.Trim();
                    return $"📦 **Check Stock for '{name}':**\n• Use the search box in the UI\n• Or call: `GET /items?search={name}`\n• This will show current quantity and reorder levels";
                }
            }

            // Deduction with quantity and item
            var deductMatch = Regex.Match(m, @"(?:deduct|use|take|remove)\s+(\d+)\s+(?:from\s+)?([a-zA-Z\s\-]+)");
            if (deductMatch.Success)
            {
                var amount = deductMatch.Groups[1].Value;
                var name = deductMatch.Groups[2].Value.Trim();
                return $"➖ **Deduct {amount} from '{name}':**\n1. Find the item ID first: `GET /items?search={name}`\n2. Record usage: `POST /items/{{id}}/deduct`\n3. Body: `{{ \"amount\": {amount}, \"reason\": \"Patient care\", \"user\": \"your-name\" }}`";
            }

            // General deduction
            if (m.Contains("deduct") || m.Contains("use") || m.Contains("take") || m.Contains("remove"))
                return "➖ **Record Item Usage:**\n• Find item: Search by name or ID\n• Call: `POST /items/{id}/deduct`\n• Include: amount, reason, and your name\n• Example: `{ \"amount\": 5, \"reason\": \"Emergency Room\", \"user\": \"Dr. Smith\" }`";

            // Low stock
            if (m.Contains("low stock") || m.Contains("reorder") || m.Contains("shortage"))
                return "⚠️ **Low Stock Management:**\n• Check current low stock: `GET /items` with quantity filters\n• Items below reorder level will be flagged\n• Set reorder levels when creating/updating items\n• Use search filters to find specific categories";

            // Authentication
            if (m.Contains("login") || m.Contains("auth") || m.Contains("token") || m.Contains("register"))
                return "🔐 **Authentication Flow:**\n• **Register:** `POST /auth/register` → returns JWT token\n• **Login:** `POST /auth/login` → returns JWT token\n• **Use token:** Add header `Authorization: Bearer <token>`\n• **Verify:** `GET /auth/me` to check current user";

            // Adding stock
            if (m.Contains("add") || m.Contains("restock") || m.Contains("replenish"))
                return "➕ **Add Stock:**\n• Create new item: `POST /items`\n• Update existing: `PUT /items/{id}`\n• Include name, quantity, and optional reorder level\n• Only users with 'ModifyInventory' role can add stock";

            // Bulk operations
            if (m.Contains("bulk") || m.Contains("multiple") || m.Contains("batch"))
                return "📊 **Bulk Operations:**\n• Currently handle items individually\n• For multiple items, make separate API calls\n• Consider using the UI for easier bulk management\n• Future versions may support batch endpoints";

            // Generic help
            return "🤖 **I can help with inventory management:**\n\n**Common tasks:**\n• 'stock of bandages' - Check quantities\n• 'deduct 5 syringes for ER' - Record usage\n• 'low stock items' - See what needs reordering\n• 'health check' - API status\n• 'how to login' - Authentication help\n\n**Try asking about specific items or operations!**";
        }
    }
}