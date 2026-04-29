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

    [JsonPropertyName("CustomerEmail")]
    public string? CustomerEmail { get; set; }

    [JsonPropertyName("Sku")]
    public string? Sku { get; set; }

    [JsonPropertyName("CustomerName")]
    public string? RentalCustomerName { get; set; }

    [JsonPropertyName("StatusName")]
    public string? StatusName { get; set; }

    [JsonPropertyName("Status")]
    public string? Status { get; set; }

    [JsonPropertyName("PaymentStatus")]
    public int PaymentStatus { get; set; }

    [JsonPropertyName("ShippingStatus")]
    public int ShippingStatus { get; set; }

    [JsonPropertyName("TotalGrand")]
    public decimal TotalGrand { get; set; }

    [JsonPropertyName("Quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("DailyPrice")]
    public decimal DailyPrice { get; set; }

    [JsonPropertyName("TotalPrice")]
    public decimal TotalPrice { get; set; }

    [JsonPropertyName("DepositAmount")]
    public decimal DepositAmount { get; set; }

    [JsonPropertyName("InternalNotes")]
    public string? InternalNotes { get; set; }

    [JsonPropertyName("TimeOfOrderUtc")]
    public string? TimeOfOrderUtcRaw { get; set; }

    [JsonPropertyName("RentalStart")]
    public string? RentalStartRaw { get; set; }

    [JsonPropertyName("RentalEnd")]
    public string? RentalEndRaw { get; set; }

    [JsonPropertyName("BillingAddress")]
    public Address? BillingAddress { get; set; }

    [JsonIgnore]
    public DateTime? TimeOfOrderUtc => HotcakesDateParser.Parse(TimeOfOrderUtcRaw);

    [JsonIgnore]
    public DateTime? RentalStart => HotcakesDateParser.Parse(RentalStartRaw);

    [JsonIgnore]
    public DateTime? RentalEnd => HotcakesDateParser.Parse(RentalEndRaw);

    [JsonIgnore]
    public string DisplayEmail
    {
        get
        {
            var bestEmail = !string.IsNullOrWhiteSpace(CustomerEmail)
                ? CustomerEmail
                : !string.IsNullOrWhiteSpace(UserEmail)
                    ? UserEmail
                    : null;

            if (string.Equals(Status, "Draft", StringComparison.OrdinalIgnoreCase))
                return bestEmail ?? "nem bejelentkezett felhasználó";

            return bestEmail ?? "Ismeretlen";
        }
    }

    [JsonIgnore]
    public string RentalStartText => RentalStart?.ToString("yyyy.MM.dd") ?? string.Empty;

    [JsonIgnore]
    public string RentalEndText => RentalEnd?.ToString("yyyy.MM.dd") ?? string.Empty;

    [JsonIgnore]
    public string CurrentRentalStatus { get; private set; } = string.Empty;

    [JsonIgnore]
    public string ApiStatusDisplay => Status ?? StatusName ?? string.Empty;

    [JsonIgnore]
    public string ProductName { get; set; } = string.Empty;

    [JsonIgnore]
    public int RentalDays
    {
        get
        {
            if (!RentalStart.HasValue || !RentalEnd.HasValue)
                return 0;

            return Math.Max(1, (RentalEnd.Value.Date - RentalStart.Value.Date).Days + 1);
        }
    }

    public void UpdateCurrentRentalStatus(DateTime currentTime)
    {
        if (string.Equals(Status, "Draft", StringComparison.OrdinalIgnoreCase))
        {
            CurrentRentalStatus = "Kosárba rakva, rendelésre vár";
            return;
        }

        var isPaid = string.Equals(Status, "Paid", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(StatusName, "Paid", StringComparison.OrdinalIgnoreCase);

        if (!isPaid)
        {
            CurrentRentalStatus = ApiStatusDisplay;
            return;
        }

        var currentDate = currentTime.Date;
        var rentalStartDate = RentalStart?.Date;
        var rentalEndDate = RentalEnd?.Date;

        if (rentalEndDate.HasValue && currentDate > rentalEndDate.Value)
        {
            CurrentRentalStatus = "Befejezett";
            return;
        }

        if (rentalStartDate.HasValue && rentalEndDate.HasValue && currentDate >= rentalStartDate.Value && currentDate <= rentalEndDate.Value)
        {
            CurrentRentalStatus = "Aktív";
            return;
        }

        if (rentalStartDate.HasValue && currentDate < rentalStartDate.Value)
        {
            CurrentRentalStatus = "Függőben";
            return;
        }

        CurrentRentalStatus = ApiStatusDisplay;
    }

    [JsonIgnore]
    public string CustomerName =>
        !string.IsNullOrWhiteSpace(RentalCustomerName)
            ? RentalCustomerName
            : !string.IsNullOrWhiteSpace(BillingAddress?.FullName)
            ? BillingAddress.FullName
            : DisplayEmail;
}
