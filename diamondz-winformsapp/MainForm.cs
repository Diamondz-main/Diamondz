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
        MinimumSize = new Size(1400, 820);
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

        _dashboardButton.Click += (_, _) => ShowControl(_dashboardControl, _dashboardButton);
        _ordersButton.Click += (_, _) => ShowControl(_ordersControl, _ordersButton);
        _productsButton.Click += (_, _) => ShowControl(_productsControl, _productsButton);

        _refreshButton = new Button
        {
            Text = "Friss\u00edt\u00e9s",
            Size = new Size(120, 40),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(202, 162, 107),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom
        };
        _refreshButton.FlatAppearance.BorderSize = 0;
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
        _productsControl.SaveRequested += SaveProductAsync;
        _productsControl.EnsureMissingInventoryRequested += EnsureMissingInventoryAsync;

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
        _logoLabel.Size = new Size(_sidebar.Width, 54);
        _logoLabel.Location = new Point(0, 6);
        _refreshButton.Location = new Point(28, _sidebar.Height - 112);
        _statusLabel.Location = new Point(28, _sidebar.Height - 66);
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
        return button;
    }

    private void ShowControl(Control controlToShow, Button activeButton)
    {
        foreach (Control control in _contentPanel.Controls)
            control.Visible = false;

        foreach (var button in new[] { _dashboardButton, _ordersButton, _productsButton })
        {
            button.BackColor = Color.FromArgb(28, 34, 74);
            button.ForeColor = Color.White;
        }

        activeButton.BackColor = Color.FromArgb(202, 162, 107);
        activeButton.ForeColor = Color.FromArgb(28, 34, 74);
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

            _dashboardControl.BindData(_products, _orders);
            _productsControl.BindData(_products);
            _ordersControl.BindData(_orders);

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

    private async Task EnsureMissingInventoryAsync()
    {
        var missingProducts = _products
            .Where(p => !string.IsNullOrWhiteSpace(p.Bvin) && string.IsNullOrWhiteSpace(p.InventoryBvin))
            .ToList();

        if (missingProducts.Count == 0)
        {
            MessageBox.Show(
                "Nincs olyan term\u00e9k, amelyhez hi\u00e1nyozna inventory rekord.",
                "Inventory felt\u00f6lt\u00e9s",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        var confirmation = MessageBox.Show(
            $"{missingProducts.Count} term\u00e9khez hi\u00e1nyzik inventory rekord. L\u00e9trehozzam ezeket, \u00e9s \u00e1ll\u00edtsam az On Hand \u00e9rt\u00e9ket 5-re?",
            "Inventory felt\u00f6lt\u00e9s",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirmation != DialogResult.Yes)
            return;

        try
        {
            SetLoadingState(true, $"Hi\u00e1nyz\u00f3 inventoryk l\u00e9trehoz\u00e1sa: 0/{missingProducts.Count}");

            var processed = 0;
            foreach (var product in missingProducts)
            {
                product.InventoryQuantity = 5;
                product.IsAvailableForSale = true;
                product.Status = 1;

                await _apiClient.SaveProductAsync(product);

                processed++;
                SetLoadingState(true, $"Hi\u00e1nyz\u00f3 inventoryk l\u00e9trehoz\u00e1sa: {processed}/{missingProducts.Count}");
            }

            await LoadDataAsync();

            MessageBox.Show(
                $"{processed} term\u00e9k inventory rekordja l\u00e9trej\u00f6tt, \u00e9s az On Hand \u00e9rt\u00e9k 5 lett.",
                "Inventory felt\u00f6lt\u00e9s",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Nem siker\u00fclt minden hi\u00e1nyz\u00f3 inventory rekordot l\u00e9trehozni.\n\n{ex.Message}",
                "Inventory felt\u00f6lt\u00e9si hiba",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            SetLoadingState(false, $"Bet\u00f6ltve: {_products.Count} term\u00e9k");
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