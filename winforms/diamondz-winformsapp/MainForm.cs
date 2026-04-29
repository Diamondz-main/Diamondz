using DiamondzWinForms.Controls;
using DiamondzWinForms.Models;
using DiamondzWinForms.Services;

namespace DiamondzWinForms;

public class MainForm : Form
{
    private readonly Panel _sidebar;
    private readonly Panel _contentPanel;
    private readonly Button _dashboardButton;
    private readonly Button _ordersButton;
    private readonly Button _productsButton;
    private readonly Button _refreshButton;
    private readonly Label _statusLabel;
    private readonly Label _logoLabel;

    private readonly DashboardControl _dashboardControl;
    private readonly OrdersControl _ordersControl;
    private readonly ProductsControl _productsControl;
    private readonly HotcakesApiClient _apiClient;

    private List<Product> _products = new();
    private List<Order> _orders = new();

    public MainForm()
    {
        Text = "Diamondz Admin";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1100, 720);
        Size = new Size(1500, 900);
        BackColor = Color.FromArgb(246, 243, 239);
        WindowState = FormWindowState.Maximized;

        _apiClient = new HotcakesApiClient();

        _sidebar = new Panel
        {
            Dock = DockStyle.Left,
            Width = 240,
            BackColor = Color.FromArgb(28, 34, 74)
        };

        _logoLabel = new Label
        {
            Text = "DIAMONDZ",
            Font = new Font("Georgia", 20, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = false,
            Size = new Size(240, 54),
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(0, 6)
        };

        _dashboardButton = CreateMenuButton("Dashboard", 120);
        _ordersButton = CreateMenuButton("K\u00f6lcs\u00f6nz\u00e9sek", 185);
        _productsButton = CreateMenuButton("Term\u00e9kek", 250);

        _refreshButton = new Button
        {
            Text = "Adatok \u00fajrat\u00f6lt\u00e9se",
            Size = new Size(184, 40),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(202, 162, 107),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom
        };
        _refreshButton.FlatAppearance.BorderSize = 0;
        _refreshButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(220, 180, 124);
        _refreshButton.Click += async (_, _) => await LoadDataAsync();

        _statusLabel = new Label
        {
            Text = "Bet\u00f6ltve: 0 term\u00e9k",
            ForeColor = Color.WhiteSmoke,
            AutoSize = false,
            Size = new Size(200, 48),
            TextAlign = ContentAlignment.TopLeft,
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom
        };

        _sidebar.Controls.Add(_logoLabel);
        _sidebar.Controls.Add(_dashboardButton);
        _sidebar.Controls.Add(_ordersButton);
        _sidebar.Controls.Add(_productsButton);
        _sidebar.Controls.Add(_refreshButton);
        _sidebar.Controls.Add(_statusLabel);

        _contentPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(246, 243, 239)
        };

        _dashboardControl = new DashboardControl();
        _ordersControl = new OrdersControl();
        _productsControl = new ProductsControl();
        _dashboardControl.AllProductsRequested += () => ShowProducts(_productsControl.ShowAllProducts);
        _dashboardControl.AvailableProductsRequested += () => ShowProducts(_productsControl.ShowAvailableProducts);
        _dashboardControl.PurchasableProductsRequested += () => ShowProducts(_productsControl.ShowPurchasableProducts);
        _dashboardControl.LowStockProductsRequested += () => ShowProducts(_productsControl.ShowLowStockProducts);
        _dashboardControl.OutOfStockPurchasableProductsRequested += () => ShowProducts(_productsControl.ShowOutOfStockPurchasableProducts);
        _dashboardControl.RentableProductsRequested += () => ShowProducts(_productsControl.ShowRentableProducts);
        _dashboardControl.AllOrdersRequested += () => ShowOrders(_ordersControl.ShowPaidOrders);
        _dashboardControl.CompletedOrdersRequested += () => ShowOrders(_ordersControl.ShowExpiredOrders);
        _dashboardControl.PendingOrdersRequested += () => ShowOrders(_ordersControl.ShowPendingOrders);
        _dashboardControl.ActiveOrdersRequested += () => ShowOrders(_ordersControl.ShowActiveOrders);
        _productsControl.SaveRequested += SaveProductAsync;

        _dashboardButton.Click += (_, _) => ShowControl(_dashboardControl, _dashboardButton);
        _ordersButton.Click += (_, _) => ShowControl(_ordersControl, _ordersButton);
        _productsButton.Click += (_, _) => ShowControl(_productsControl, _productsButton);

        _contentPanel.Controls.Add(_dashboardControl);
        _contentPanel.Controls.Add(_ordersControl);
        _contentPanel.Controls.Add(_productsControl);

        Controls.Add(_contentPanel);
        Controls.Add(_sidebar);

        _sidebar.Resize += (_, _) => LayoutSidebar();
        LayoutSidebar();

        ShowControl(_dashboardControl, _dashboardButton);
        Shown += async (_, _) => await LoadDataAsync();
    }

    private void LayoutSidebar()
    {
        var compact = _sidebar.Width < 210;

        _logoLabel.Size = new Size(_sidebar.Width, 54);
        _logoLabel.Location = new Point(0, 6);
        _logoLabel.Font = compact ? new Font("Georgia", 16, FontStyle.Bold) : new Font("Georgia", 20, FontStyle.Bold);

        var buttonWidth = Math.Max(150, _sidebar.Width - 56);
        foreach (var button in new[] { _dashboardButton, _ordersButton, _productsButton })
        {
            button.Width = buttonWidth;
            button.Left = Math.Max(10, (_sidebar.Width - buttonWidth) / 2);
            button.Font = compact ? new Font("Segoe UI", 10, FontStyle.Bold) : new Font("Segoe UI", 12, FontStyle.Bold);
            button.Padding = compact ? new Padding(12, 0, 0, 0) : new Padding(18, 0, 0, 0);
        }

        _refreshButton.Width = buttonWidth;
        _refreshButton.Location = new Point(Math.Max(10, (_sidebar.Width - buttonWidth) / 2), _sidebar.Height - 112);
        _refreshButton.Font = compact ? new Font("Segoe UI", 8.5f, FontStyle.Bold) : new Font("Segoe UI", 10, FontStyle.Bold);
        _statusLabel.Location = new Point(28, _sidebar.Height - 66);
        _statusLabel.Size = new Size(_sidebar.Width - 40, 48);
        _statusLabel.Font = compact ? new Font("Segoe UI", 8.5f, FontStyle.Regular) : new Font("Segoe UI", 9f, FontStyle.Regular);
    }

    private Button CreateMenuButton(string text, int top)
    {
        var button = new Button
        {
            Text = text,
            Size = new Size(210, 52),
            Location = new Point(15, top),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            BackColor = Color.FromArgb(28, 34, 74),
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(18, 0, 0, 0)
        };

        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(47, 55, 104);
        return button;
    }

    private void ShowProducts(Action applyFilter)
    {
        ShowControl(_productsControl, _productsButton);
        applyFilter();
    }

    private void ShowOrders(Action applyFilter)
    {
        ShowControl(_ordersControl, _ordersButton);
        applyFilter();
    }

    private void ShowControl(Control controlToShow, Button activeButton)
    {
        foreach (Control control in _contentPanel.Controls)
            control.Visible = false;

        foreach (var button in new[] { _dashboardButton, _ordersButton, _productsButton })
        {
            button.BackColor = Color.FromArgb(28, 34, 74);
            button.ForeColor = Color.White;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(47, 55, 104);
        }

        activeButton.BackColor = Color.FromArgb(202, 162, 107);
        activeButton.ForeColor = Color.FromArgb(28, 34, 74);
        activeButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(220, 180, 124);
        controlToShow.Visible = true;
        controlToShow.BringToFront();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            SetLoadingState(true, "Adatok bet\u00f6lt\u00e9se...");

            _products = await _apiClient.GetProductsAsync();
            _orders = await _apiClient.GetOrdersAsync();
            ApplyRentalAvailabilityToProducts(_products, _orders);

            _dashboardControl.BindData(_products, _orders);
            _productsControl.BindData(_products);
            _ordersControl.BindData(_orders, _products);

            SetLoadingState(false, $"Bet\u00f6ltve: {_products.Count} term\u00e9k");
        }
        catch (Exception ex)
        {
            SetLoadingState(false, "Hiba t\u00f6rt\u00e9nt");
            MessageBox.Show(
                $"Nem siker\u00fclt bet\u00f6lteni az adatokat.\n\n{ex.Message}\n\nEllen\u0151rizd a BaseUrl \u00e9s ApiKey \u00e9rt\u00e9keket az ApiSettings.cs f\u00e1jlban.",
                "API hiba",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private async Task SaveProductAsync(Product product)
    {
        try
        {
            SetLoadingState(true, "Ment\u00e9s...");

            await _apiClient.SaveProductAsync(product);
            await LoadDataAsync();

            MessageBox.Show("A term\u00e9k ment\u00e9se sikeres volt.", "Ment\u00e9s", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Nem siker\u00fclt menteni a term\u00e9ket.\n\n{ex.Message}",
                "Ment\u00e9si hiba",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            SetLoadingState(false, $"Bet\u00f6ltve: {_products.Count} term\u00e9k");
        }
    }

    private static void ApplyRentalAvailabilityToProducts(List<Product> products, List<Order> orders)
    {
        var activeRentalSkus = orders
            .Where(x =>
                (string.Equals(x.Status, "Paid", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(x.StatusName, "Paid", StringComparison.OrdinalIgnoreCase)) &&
                string.Equals(x.CurrentRentalStatus, "Aktív", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(x.Sku))
            .Select(x => x.Sku!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var product in products.Where(x => x.IsRentableProduct && !string.IsNullOrWhiteSpace(x.Sku)))
        {
            var isCurrentlyRented = activeRentalSkus.Contains(product.Sku!);
            product.InventoryOnHandQuantity = isCurrentlyRented ? 0 : 1;
            product.InventoryReservedQuantity = 0;
            product.InventoryQuantity = isCurrentlyRented ? 0 : 1;
            product.IsAvailableForSale = !isCurrentlyRented;
            product.Status = isCurrentlyRented ? 0 : 1;
        }
    }

    private void SetLoadingState(bool isLoading, string statusText)
    {
        _refreshButton.Enabled = !isLoading;
        _dashboardButton.Enabled = !isLoading;
        _ordersButton.Enabled = !isLoading;
        _productsButton.Enabled = !isLoading;
        _statusLabel.Text = statusText;
        Cursor = isLoading ? Cursors.WaitCursor : Cursors.Default;
    }
}
