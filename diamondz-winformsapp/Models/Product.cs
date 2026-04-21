using System.Text.Json;
using System.Text.Json.Serialization;

namespace DiamondzWinForms.Models;

public class Product
{
    [JsonPropertyName("Bvin")]
    public string? Bvin { get; set; }

    [JsonPropertyName("Sku")]
    public string? Sku { get; set; }

    [JsonPropertyName("ProductName")]
    public string? ProductName { get; set; }

    [JsonPropertyName("ProductTypeId")]
    public string? ProductTypeId { get; set; }

    [JsonPropertyName("SitePrice")]
    public decimal SitePrice { get; set; }

    [JsonPropertyName("ImageFileSmall")]
    public string? ImageFileSmall { get; set; }

    [JsonPropertyName("Status")]
    public int Status { get; set; }

    [JsonPropertyName("InventoryMode")]
    public int InventoryMode { get; set; }

    [JsonPropertyName("IsAvailableForSale")]
    public bool IsAvailableForSale { get; set; }

    [JsonIgnore]
    public string? InventoryBvin { get; set; }

    [JsonIgnore]
    public int InventoryQuantity { get; set; }

    [JsonIgnore]
    public bool EffectiveIsAvailable => InventoryQuantity > 0;

    [JsonIgnore]
    public string AvailabilityText => EffectiveIsAvailable ? "El\u00e9rhet\u0151" : "Nem el\u00e9rhet\u0151";

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtraData { get; set; }
}