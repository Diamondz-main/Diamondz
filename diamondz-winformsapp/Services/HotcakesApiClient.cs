using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using DiamondzWinForms.Models;

namespace DiamondzWinForms.Services;

public class HotcakesApiClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    private static readonly Regex HotcakesDateRegex =
        new(@"^/Date\((\-?\d+)\)/$", RegexOptions.Compiled);

    public HotcakesApiClient()
    {
        _httpClient = new HttpClient();
    }

    public async Task<List<Product>> GetProductsAsync()
    {
        using var response = await _httpClient.GetAsync(ApiSettings.ProductsEndpoint);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var products = DeserializeList<Product>(json, "ProductName", "Bvin");

        var inventoryMap = await TryGetInventoryMapAsync(products
            .Select(p => p.Bvin)
            .Where(b => !string.IsNullOrWhiteSpace(b))
            .Select(b => b!));

        foreach (var product in products)
        {
            ApplyInventoryState(product, inventoryMap.TryGetValue(product.Bvin ?? string.Empty, out var inventory) ? inventory : null);
        }

        return products;
    }

    public async Task<List<Order>> GetOrdersAsync()
    {
        using var response = await _httpClient.GetAsync(ApiSettings.OrdersEndpoint);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return DeserializeList<Order>(json, "OrderNumber", "Id");
    }

    public async Task SaveProductAsync(Product product)
    {
        if (string.IsNullOrWhiteSpace(product.Bvin))
            throw new InvalidOperationException("A term\u00e9k BVIN \u00e9rt\u00e9ke hi\u00e1nyzik.");

        product.Status = product.InventoryQuantity > 0 ? 1 : 0;
        product.IsAvailableForSale = product.InventoryQuantity > 0;

        await UpdateProductCoreAsync(product);
        await SaveInventoryCoreAsync(product);
        await TryReindexProductAsync(product.Bvin);
    }

    public async Task<Product?> GetProductAsync(string productBvin)
    {
        if (string.IsNullOrWhiteSpace(productBvin))
            return null;

        using var response = await _httpClient.GetAsync(ApiSettings.ProductByBvinEndpoint(productBvin));
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var product = DeserializeSingle<Product>(json, "ProductName", "Bvin");
        if (product is null)
            return null;

        var inventory = await TryGetInventoryForProductAsync(productBvin);
        ApplyInventoryState(product, inventory);
        return product;
    }

    public async Task<Product?> WaitForProductStateAsync(Product expectedProduct, int attempts = 5, int delayMs = 400)
    {
        if (string.IsNullOrWhiteSpace(expectedProduct.Bvin))
            return null;

        Product? lastProduct = null;

        for (var attempt = 0; attempt < attempts; attempt++)
        {
            lastProduct = await GetProductAsync(expectedProduct.Bvin);
            if (lastProduct is not null &&
                lastProduct.SitePrice == expectedProduct.SitePrice &&
                lastProduct.InventoryQuantity == expectedProduct.InventoryQuantity &&
                lastProduct.Status == expectedProduct.Status)
            {
                return lastProduct;
            }

            await Task.Delay(delayMs);
        }

        return lastProduct;
    }

    private async Task UpdateProductCoreAsync(Product editedProduct)
    {
        var getUrl = ApiSettings.ProductByBvinEndpoint(editedProduct.Bvin!);
        using var getResponse = await _httpClient.GetAsync(getUrl);
        var getJson = await getResponse.Content.ReadAsStringAsync();
        getResponse.EnsureSuccessStatusCode();

        var rootNode = JsonNode.Parse(getJson) ?? throw new Exception("Nem sikerĂĽlt feldolgozni a termĂ©k GET vĂˇlaszĂˇt.");
        var productNode = FindObjectNode(rootNode, "Bvin", "ProductName") ?? throw new Exception("A termĂ©k objektum nem talĂˇlhatĂł a GET vĂˇlaszban.");

        productNode["SitePrice"] = editedProduct.SitePrice;
        productNode["Status"] = editedProduct.Status;
        productNode["IsAvailableForSale"] = editedProduct.IsAvailableForSale;

        NormalizeHotcakesDates(productNode);
        var bodyJson = productNode.ToJsonString(_jsonOptions);

        var urls = new[]
        {
            ApiSettings.ProductByBvinEndpoint(editedProduct.Bvin!),
            ApiSettings.ProductsEndpoint,
            ApiSettings.ProductsUpdateEndpoint
        };

        await PostJsonToFirstWorkingEndpointAsync(urls, bodyJson, "termĂ©k frissĂ­tĂ©se");
    }

    private async Task SaveInventoryCoreAsync(Product product)
    {
        var inventoryNode = await GetInventoryNodeForSaveAsync(product);

        inventoryNode["ProductBvin"] = product.Bvin;
        inventoryNode["VariantId"] = inventoryNode["VariantId"]?.ToString() ?? string.Empty;
        inventoryNode["QuantityOnHand"] = product.InventoryQuantity;
        inventoryNode["QuantityReserved"] = ParseIntNode(inventoryNode["QuantityReserved"]);
        inventoryNode["LowStockPoint"] = ParseIntNode(inventoryNode["LowStockPoint"]);
        inventoryNode["OutOfStockPoint"] = ParseIntNode(inventoryNode["OutOfStockPoint"]);

        NormalizeHotcakesDates(inventoryNode);
        var bodyJson = inventoryNode.ToJsonString(_jsonOptions);

        var urls = new List<string>();
        if (!string.IsNullOrWhiteSpace(product.InventoryBvin))
        {
            urls.Add(ApiSettings.ProductInventoryByBvinEndpoint(product.InventoryBvin));
        }

        urls.Add(ApiSettings.ProductInventoryEndpoint);
        urls.Add(ApiSettings.ProductInventoryUpdateEndpoint);

        await PostJsonToFirstWorkingEndpointAsync(urls, bodyJson, "inventory frissĂ­tĂ©se");
    }

    private async Task<JsonObject> GetInventoryNodeForSaveAsync(Product product)
    {
        if (!string.IsNullOrWhiteSpace(product.InventoryBvin))
        {
            var existingById = await TryGetJsonNodeAsync(ApiSettings.ProductInventoryByBvinEndpoint(product.InventoryBvin));
            var objectById = existingById is null ? null : FindObjectNode(existingById, "Bvin", "QuantityOnHand");
            if (objectById is not null)
            {
                return objectById;
            }
        }

        if (!string.IsNullOrWhiteSpace(product.Bvin))
        {
            foreach (var url in new[]
                     {
                         ApiSettings.ProductInventoryForProductEndpoint(product.Bvin),
                         ApiSettings.ProductInventoryForProductQueryEndpoint(product.Bvin)
                     })
            {
                var existingForProduct = await TryGetJsonNodeAsync(url);
                var objectForProduct = existingForProduct is null ? null : FindObjectNode(existingForProduct, "ProductBvin", "QuantityOnHand");
                if (objectForProduct is not null)
                {
                    if (string.IsNullOrWhiteSpace(product.InventoryBvin))
                    {
                        product.InventoryBvin = objectForProduct["Bvin"]?.ToString();
                    }

                    return objectForProduct;
                }
            }
        }

        return new JsonObject
        {
            ["Bvin"] = string.IsNullOrWhiteSpace(product.InventoryBvin) ? string.Empty : product.InventoryBvin,
            ["ProductBvin"] = product.Bvin,
            ["VariantId"] = string.Empty,
            ["QuantityOnHand"] = product.InventoryQuantity,
            ["QuantityReserved"] = 0,
            ["LowStockPoint"] = 0,
            ["OutOfStockPoint"] = 0
        };
    }

    private async Task<Dictionary<string, ProductInventoryRecord>> TryGetInventoryMapAsync(IEnumerable<string> productBvins)
    {
        var result = new Dictionary<string, ProductInventoryRecord>(StringComparer.OrdinalIgnoreCase);

        var listResponse = await TryGetInventoryListAsync();
        if (listResponse.Count > 0)
        {
            foreach (var item in listResponse.Where(i => !string.IsNullOrWhiteSpace(i.ProductBvin)))
            {
                result[item.ProductBvin!] = item;
            }

            return result;
        }

        var uniqueBvins = productBvins.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        using var throttler = new SemaphoreSlim(8);

        var tasks = uniqueBvins.Select(async bvin =>
        {
            await throttler.WaitAsync();
            try
            {
                var item = await TryGetInventoryForProductAsync(bvin!);
                if (item is not null && !string.IsNullOrWhiteSpace(item.ProductBvin))
                {
                    lock (result)
                    {
                        result[item.ProductBvin!] = item;
                    }
                }
            }
            finally
            {
                throttler.Release();
            }
        });

        await Task.WhenAll(tasks);
        return result;
    }

    private async Task<List<ProductInventoryRecord>> TryGetInventoryListAsync()
    {
        foreach (var url in new[]
                 {
                     ApiSettings.ProductInventoryEndpoint,
                     ApiSettings.ProductInventoryFindAllEndpoint
                 })
        {
            try
            {
                using var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                    continue;

                var json = await response.Content.ReadAsStringAsync();
                var items = DeserializeList<ProductInventoryRecord>(json, "ProductBvin", "QuantityOnHand", "Bvin");
                if (items.Count > 0)
                    return items;
            }
            catch
            {
                // kĂ¶vetkezĹ‘ endpoint prĂłbĂˇlĂˇsa
            }
        }

        return new List<ProductInventoryRecord>();
    }

    private async Task<ProductInventoryRecord?> TryGetInventoryForProductAsync(string productBvin)
    {
        foreach (var url in new[]
                 {
                     ApiSettings.ProductInventoryForProductEndpoint(productBvin),
                     ApiSettings.ProductInventoryForProductQueryEndpoint(productBvin)
                 })
        {
            try
            {
                using var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                    continue;

                var json = await response.Content.ReadAsStringAsync();

                var list = DeserializeList<ProductInventoryRecord>(json, "ProductBvin", "QuantityOnHand", "Bvin");
                if (list.Count > 0)
                    return list.FirstOrDefault(x => string.Equals(x.ProductBvin, productBvin, StringComparison.OrdinalIgnoreCase));

                var single = DeserializeSingle<ProductInventoryRecord>(json, "ProductBvin", "QuantityOnHand", "Bvin");
                if (single is not null)
                    return single;
            }
            catch
            {
                // kĂ¶vetkezĹ‘ endpoint prĂłbĂˇlĂˇsa
            }
        }

        return null;
    }

    private async Task TryReindexProductAsync(string productBvin)
    {
        foreach (var url in new[]
                 {
                     ApiSettings.SearchManagerIndexProductEndpoint(productBvin),
                     ApiSettings.SearchManagerIndexProductQueryEndpoint(productBvin)
                 })
        {
            try
            {
                using var response = await _httpClient.PostAsync(url, new StringContent(string.Empty));
                if (response.IsSuccessStatusCode)
                    return;
            }
            catch
            {
                // az indexelĂ©s nem kritikus a mentĂ©shez
            }
        }
    }

    private async Task<JsonNode?> TryGetJsonNodeAsync(string url)
    {
        try
        {
            using var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            return JsonNode.Parse(json);
        }
        catch
        {
            return null;
        }
    }

    private async Task PostJsonToFirstWorkingEndpointAsync(IEnumerable<string> urls, string bodyJson, string operationName)
    {
        string? lastError = null;

        foreach (var url in urls.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(bodyJson, Encoding.UTF8, "application/json")
                };

                using var response = await _httpClient.SendAsync(request);
                var responseText = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    lastError = $"HTTP {(int)response.StatusCode} ({response.ReasonPhrase}) - {responseText}";
                    continue;
                }

                var apiError = ExtractApiError(responseText);
                if (!string.IsNullOrWhiteSpace(apiError))
                {
                    lastError = apiError;
                    continue;
                }

                return;
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
            }
        }

        throw new Exception($"Nem sikerĂĽlt a(z) {operationName}.\n\n{lastError}");
    }

    private List<T> DeserializeList<T>(string json, params string[] expectedKeys)
    {
        using var doc = JsonDocument.Parse(json);

        if (TryFindMatchingArray(doc.RootElement, expectedKeys, out var arrayElement))
        {
            return JsonSerializer.Deserialize<List<T>>(arrayElement.GetRawText(), _jsonOptions) ?? new List<T>();
        }

        return new List<T>();
    }

    private T? DeserializeSingle<T>(string json, params string[] expectedKeys)
    {
        using var doc = JsonDocument.Parse(json);

        if (TryFindMatchingObject(doc.RootElement, expectedKeys, out var objectElement))
        {
            return JsonSerializer.Deserialize<T>(objectElement.GetRawText(), _jsonOptions);
        }

        return default;
    }

    private bool TryFindMatchingArray(JsonElement element, string[] expectedKeys, out JsonElement arrayElement)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            if (LooksLikeTargetArray(element, expectedKeys))
            {
                arrayElement = element;
                return true;
            }
        }

        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var name in new[] { "Content", "Items", "Data", "Products", "Orders", "Results" })
            {
                if (element.TryGetProperty(name, out var child) && TryFindMatchingArray(child, expectedKeys, out arrayElement))
                {
                    return true;
                }
            }

            foreach (var property in element.EnumerateObject())
            {
                if (TryFindMatchingArray(property.Value, expectedKeys, out arrayElement))
                {
                    return true;
                }
            }
        }

        arrayElement = default;
        return false;
    }

    private bool TryFindMatchingObject(JsonElement element, string[] expectedKeys, out JsonElement objectElement)
    {
        if (element.ValueKind == JsonValueKind.Object && LooksLikeTargetObject(element, expectedKeys))
        {
            objectElement = element;
            return true;
        }

        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var name in new[] { "Content", "Item", "Data", "Result" })
            {
                if (element.TryGetProperty(name, out var child) && TryFindMatchingObject(child, expectedKeys, out objectElement))
                {
                    return true;
                }
            }

            foreach (var property in element.EnumerateObject())
            {
                if (TryFindMatchingObject(property.Value, expectedKeys, out objectElement))
                {
                    return true;
                }
            }
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                if (TryFindMatchingObject(item, expectedKeys, out objectElement))
                {
                    return true;
                }
            }
        }

        objectElement = default;
        return false;
    }

    private static bool LooksLikeTargetArray(JsonElement arrayElement, string[] expectedKeys)
    {
        if (arrayElement.GetArrayLength() == 0)
            return true;

        var first = arrayElement[0];
        if (first.ValueKind != JsonValueKind.Object)
            return false;

        return LooksLikeTargetObject(first, expectedKeys);
    }

    private static bool LooksLikeTargetObject(JsonElement objectElement, string[] expectedKeys)
    {
        if (objectElement.ValueKind != JsonValueKind.Object)
            return false;

        foreach (var key in expectedKeys)
        {
            if (objectElement.TryGetProperty(key, out _))
                return true;
        }

        return false;
    }

    private static JsonObject? FindObjectNode(JsonNode node, params string[] expectedKeys)
    {
        if (node is JsonObject obj)
        {
            if (expectedKeys.Any(key => obj.ContainsKey(key)))
                return obj;

            foreach (var key in new[] { "Content", "Item", "Data", "Result" })
            {
                if (obj[key] is JsonNode child)
                {
                    var found = FindObjectNode(child, expectedKeys);
                    if (found is not null)
                        return found;
                }
            }

            foreach (var kv in obj)
            {
                if (kv.Value is JsonNode child)
                {
                    var found = FindObjectNode(child, expectedKeys);
                    if (found is not null)
                        return found;
                }
            }
        }

        if (node is JsonArray array)
        {
            foreach (var item in array)
            {
                if (item is JsonNode child)
                {
                    var found = FindObjectNode(child, expectedKeys);
                    if (found is not null)
                        return found;
                }
            }
        }

        return null;
    }

    private static void NormalizeHotcakesDates(JsonNode? node)
    {
        if (node is JsonObject obj)
        {
            var keys = obj.Select(x => x.Key).ToList();
            foreach (var key in keys)
            {
                var child = obj[key];

                if (child is JsonValue value && value.TryGetValue<string>(out var s))
                {
                    var converted = ConvertHotcakesDateString(s);
                    if (converted is not null)
                    {
                        obj[key] = converted;
                    }
                }
                else
                {
                    NormalizeHotcakesDates(child);
                }
            }
        }
        else if (node is JsonArray arr)
        {
            for (int i = 0; i < arr.Count; i++)
            {
                var child = arr[i];
                if (child is JsonValue value && value.TryGetValue<string>(out var s))
                {
                    var converted = ConvertHotcakesDateString(s);
                    if (converted is not null)
                    {
                        arr[i] = converted;
                    }
                }
                else
                {
                    NormalizeHotcakesDates(child);
                }
            }
        }
    }

    private static string? ConvertHotcakesDateString(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var match = HotcakesDateRegex.Match(value);
        if (!match.Success)
            return null;

        var milliseconds = long.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        return DateTimeOffset.FromUnixTimeMilliseconds(milliseconds).UtcDateTime.ToString("o", CultureInfo.InvariantCulture);
    }

    private static string? ExtractApiError(string responseText)
    {
        try
        {
            var node = JsonNode.Parse(responseText);
            var errors = node?["Errors"] as JsonArray;
            if (errors is null || errors.Count == 0)
                return null;

            var messages = new List<string>();
            foreach (var err in errors)
            {
                var code = err?["Code"]?.ToString();
                var description = err?["Description"]?.ToString();
                if (!string.IsNullOrWhiteSpace(code) || !string.IsNullOrWhiteSpace(description))
                {
                    messages.Add($"{code}: {description}".Trim(':', ' '));
                }
            }

            return messages.Count == 0 ? null : string.Join(Environment.NewLine, messages);
        }
        catch
        {
            return null;
        }
    }

    private static int ParseIntNode(JsonNode? node)
    {
        if (node is null)
            return 0;

        if (node is JsonValue value)
        {
            if (value.TryGetValue<int>(out var i))
                return i;

            if (value.TryGetValue<long>(out var l))
                return (int)l;

            if (int.TryParse(value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                return parsed;
        }

        return 0;
    }

    private static void ApplyInventoryState(Product product, ProductInventoryRecord? inventory)
    {
        if (inventory is not null)
        {
            product.InventoryBvin = inventory.Bvin;
            product.InventoryQuantity = inventory.QuantityOnHand;
        }
        else
        {
            product.InventoryBvin = null;
            product.InventoryQuantity = 0;
        }

        product.IsAvailableForSale = product.InventoryQuantity > 0;
    }
}