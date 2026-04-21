using System.Text.Json.Serialization;
using DiamondzWinForms.Helpers;

namespace DiamondzWinForms.Models;

public class Order
{
    [JsonPropertyName("Id")]
    public int Id { get; set; }

    [JsonPropertyName("bvin")]
    public string? Bvin { get; set; }

    [JsonPropertyName("OrderNumber")]
    public string? OrderNumber { get; set; }

    [JsonPropertyName("UserEmail")]
    public string? UserEmail { get; set; }

    [JsonPropertyName("StatusName")]
    public string? StatusName { get; set; }

    [JsonPropertyName("PaymentStatus")]
    public int PaymentStatus { get; set; }

    [JsonPropertyName("ShippingStatus")]
    public int ShippingStatus { get; set; }

    [JsonPropertyName("TotalGrand")]
    public decimal TotalGrand { get; set; }

    [JsonPropertyName("TimeOfOrderUtc")]
    public string? TimeOfOrderUtcRaw { get; set; }

    [JsonPropertyName("BillingAddress")]
    public Address? BillingAddress { get; set; }

    [JsonIgnore]
    public DateTime? TimeOfOrderUtc => HotcakesDateParser.Parse(TimeOfOrderUtcRaw);

    public string CustomerName =>
        !string.IsNullOrWhiteSpace(BillingAddress?.FullName)
            ? BillingAddress.FullName
            : UserEmail ?? "Ismeretlen";
}
