using System.Text.Json.Serialization;

namespace DiamondzWinForms.Models;

public class Address
{
    [JsonPropertyName("FirstName")]
    public string? FirstName { get; set; }

    [JsonPropertyName("LastName")]
    public string? LastName { get; set; }

    [JsonPropertyName("Company")]
    public string? Company { get; set; }

    [JsonPropertyName("Line1")]
    public string? Line1 { get; set; }

    [JsonPropertyName("City")]
    public string? City { get; set; }

    [JsonPropertyName("PostalCode")]
    public string? PostalCode { get; set; }

    [JsonPropertyName("CountryName")]
    public string? CountryName { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();
}
