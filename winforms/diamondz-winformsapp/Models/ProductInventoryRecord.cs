using System.Text.Json;
using System.Text.Json.Serialization;

namespace DiamondzWinForms.Models;

public class ProductInventoryRecord
{
    [JsonPropertyName("Bvin")]
    public string? Bvin { get; set; }

    [JsonPropertyName("ProductBvin")]
    public string? ProductBvin { get; set; }

    [JsonPropertyName("VariantId")]
    public string? VariantId { get; set; }

    [JsonPropertyName("QuantityOnHand")]
    public int QuantityOnHand { get; set; }

    [JsonPropertyName("QuantityReserved")]
    public int QuantityReserved { get; set; }

    [JsonPropertyName("LowStockPoint")]
    public int LowStockPoint { get; set; }

    [JsonPropertyName("OutOfStockPoint")]
    public int OutOfStockPoint { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtraData { get; set; }
}
