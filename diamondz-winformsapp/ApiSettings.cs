namespace DiamondzWinForms;

public static class ApiSettings
{
    public static string BaseUrl { get; set; } = "http://20.238.21.118";
    public static string ApiKey { get; set; } = "1-0f3233a3-8d1d-469c-8573-b66a6cdf5e81";

    private static string ApiRoot => $"{BaseUrl.TrimEnd('/')}/DesktopModules/Hotcakes/API/rest/v1";

    public static string ProductsEndpoint => $"{ApiRoot}/products?key={ApiKey}";
    public static string ProductByBvinEndpoint(string bvin) => $"{ApiRoot}/products/{bvin}?key={ApiKey}";
    public static string ProductsUpdateEndpoint => $"{ApiRoot}/products/update?key={ApiKey}";

    public static string OrdersEndpoint => $"{ApiRoot}/orders?key={ApiKey}";

    public static string ProductInventoryEndpoint => $"{ApiRoot}/productinventory?key={ApiKey}";
    public static string ProductInventoryFindAllEndpoint => $"{ApiRoot}/productinventory/findall?key={ApiKey}";
    public static string ProductInventoryByBvinEndpoint(string bvin) => $"{ApiRoot}/productinventory/{bvin}?key={ApiKey}";
    public static string ProductInventoryForProductEndpoint(string productBvin) => $"{ApiRoot}/productinventory/forproduct/{productBvin}?key={ApiKey}";
    public static string ProductInventoryForProductQueryEndpoint(string productBvin) => $"{ApiRoot}/productinventory/forproduct?productBvin={productBvin}&key={ApiKey}";
    public static string ProductInventoryUpdateEndpoint => $"{ApiRoot}/productinventory/update?key={ApiKey}";

    public static string SearchManagerIndexProductEndpoint(string productBvin) => $"{ApiRoot}/searchmanager/indexproduct/{productBvin}?key={ApiKey}";
    public static string SearchManagerIndexProductQueryEndpoint(string productBvin) => $"{ApiRoot}/searchmanager/indexproduct?productBvin={productBvin}&key={ApiKey}";
}
