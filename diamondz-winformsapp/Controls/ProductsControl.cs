using System.Globalization;
using DiamondzWinForms.Models;

namespace DiamondzWinForms.Controls;

public class ProductsControl : UserControl
{
    private readonly Label _titleLabel;
    private readonly Panel _filterPanel;
    private readonly Label _nameLabel;
    private readonly TextBox _nameTextBox;
    private readonly Label _skuLabel;
    private readonly TextBox _skuTextBox;
    private readonly Button _clearButton;
    private readonly Button _ensureInventoryButton;
    private readonly Button _saveButton;
    private readonly DataGridView _grid;

    private List<Product> _allProducts = new();

    public event Func<Product, Task>? SaveRequested;
    public event Func<Task>? EnsureMissingInventoryRequested;

    public ProductsControl()
    {
        Dock = DockStyle.Fill;
        BackColor = Color.FromArgb(246, 243, 239);

        _titleLabel = new Label
        {
            Text = "Term\u00e9kek",
            Font = new Font("Segoe UI", 20, FontStyle.Bold),
            ForeColor = Color.FromArgb(34, 41, 74),
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter
        };

        _filterPanel = new Panel
        {
            BackColor = Color.Transparent,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        _nameLabel = new Label
        {
            Text = "N\u00e9v:",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            AutoSize = true
        };

        _nameTextBox = new TextBox();
        _nameTextBox.TextChanged += (_, _) => ApplyFilters();

        _skuLabel = new Label
        {
            Text = "SKU:",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            AutoSize = true
        };

        _skuTextBox = new TextBox();
        _skuTextBox.TextChanged += (_, _) => ApplyFilters();

        _clearButton = CreateActionButton("Sz\u0171r\u0151k t\u00f6rl\u00e9se", 120, Color.FromArgb(202, 162, 107));
        _clearButton.Click += (_, _) =>
        {
            _nameTextBox.Text = string.Empty;
            _skuTextBox.Text = string.Empty;
            ApplyFilters();
        };

        _ensureInventoryButton = CreateActionButton("Hi\u00e1nyz\u00f3 inventoryk", 160, Color.FromArgb(202, 162, 107));
        _ensureInventoryButton.Click += async (_, _) =>
        {
            if (EnsureMissingInventoryRequested is not null)
            {
                await EnsureMissingInventoryRequested.Invoke();
            }
        };

        _saveButton = CreateActionButton("Ment\u00e9s a kijel\u00f6lt sorra", 190, Color.FromArgb(34, 41, 74));
        _saveButton.Click += async (_, _) => await SaveSelectedRowAsync();

        _filterPanel.Controls.AddRange(new Control[]
        {
            _nameLabel, _nameTextBox, _skuLabel, _skuTextBox, _clearButton, _ensureInventoryButton, _saveButton
        });

        _grid = new DataGridView
        {
            ReadOnly = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            MultiSelect = false,
            AutoGenerateColumns = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            RowHeadersVisible = false,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
            ScrollBars = ScrollBars.Both
        };

        _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        _grid.DefaultCellStyle.Font = new Font("Segoe UI", 10);
        _grid.EnableHeadersVisualStyles = false;
        _grid.CellValueChanged += GridOnCellValueChanged;

        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "N\u00e9v", DataPropertyName = "ProductName", ReadOnly = true, MinimumWidth = 320 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Sku", HeaderText = "SKU", DataPropertyName = "Sku", ReadOnly = true, MinimumWidth = 240 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "SitePrice", HeaderText = "\u00c1r", ReadOnly = false, MinimumWidth = 170 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "InventoryQuantity", HeaderText = "Rakt\u00e1ron", ReadOnly = false, MinimumWidth = 110 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "AvailabilityText", HeaderText = "\u00c1llapot", ReadOnly = true, MinimumWidth = 150 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Bvin", HeaderText = "Bvin", ReadOnly = true, MinimumWidth = 340 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "InventoryBvin", HeaderText = "InventoryBvin", Visible = false, ReadOnly = true });

        Controls.Add(_titleLabel);
        Controls.Add(_filterPanel);
        Controls.Add(_grid);

        Resize += (_, _) => LayoutResponsive();
        LayoutResponsive();
    }

    private static Button CreateActionButton(string text, int width, Color backColor)
    {
        var button = new Button
        {
            Text = text,
            Size = new Size(width, 32),
            FlatStyle = FlatStyle.Flat,
            BackColor = backColor,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9, FontStyle.Bold)
        };
        button.FlatAppearance.BorderSize = 0;
        return button;
    }

    public void BindData(List<Product> products)
    {
        _allProducts = products ?? new List<Product>();
        ApplyFilters();
    }

    private void LayoutResponsive()
    {
        var left = 24;
        var gap = 12;
        var contentWidth = Math.Max(980, ClientSize.Width - (left * 2));

        _titleLabel.Location = new Point(left, 20);
        _titleLabel.Size = new Size(contentWidth, 44);

        _filterPanel.Location = new Point(left, 78);
        _filterPanel.Size = new Size(contentWidth, 50);

        var saveX = _filterPanel.Width - _saveButton.Width;
        var ensureX = saveX - gap - _ensureInventoryButton.Width;
        var clearX = ensureX - gap - _clearButton.Width;

        _nameLabel.Location = new Point(0, 14);
        _nameTextBox.Location = new Point(44, 10);
        _nameTextBox.Size = new Size(300, 27);

        _skuLabel.Location = new Point(370, 14);
        _skuTextBox.Location = new Point(416, 10);
        _skuTextBox.Size = new Size(Math.Max(150, clearX - 416 - 16), 27);

        _clearButton.Location = new Point(clearX, 8);
        _ensureInventoryButton.Location = new Point(ensureX, 8);
        _saveButton.Location = new Point(saveX, 8);

        _grid.Location = new Point(left, 140);
        _grid.Size = new Size(contentWidth, Math.Max(420, ClientSize.Height - 164));

        LayoutGridColumns();
    }

    private void LayoutGridColumns()
    {
        var availableWidth = Math.Max(1330, _grid.ClientSize.Width - 4);
        var nameWidth = 360;
        var skuWidth = 280;
        var priceWidth = 170;
        var qtyWidth = 110;
        var availabilityWidth = 150;
        var bvinWidth = Math.Max(340, availableWidth - nameWidth - skuWidth - priceWidth - qtyWidth - availabilityWidth);

        _grid.Columns["ProductName"].Width = nameWidth;
        _grid.Columns["Sku"].Width = skuWidth;
        _grid.Columns["SitePrice"].Width = priceWidth;
        _grid.Columns["InventoryQuantity"].Width = qtyWidth;
        _grid.Columns["AvailabilityText"].Width = availabilityWidth;
        _grid.Columns["Bvin"].Width = bvinWidth;
    }

    private void ApplyFilters()
    {
        var nameFilter = _nameTextBox.Text.Trim();
        var skuFilter = _skuTextBox.Text.Trim();

        var filtered = _allProducts
            .Where(p =>
                (string.IsNullOrWhiteSpace(nameFilter) || (!string.IsNullOrWhiteSpace(p.ProductName) && p.ProductName.Contains(nameFilter, StringComparison.OrdinalIgnoreCase))) &&
                (string.IsNullOrWhiteSpace(skuFilter) || (!string.IsNullOrWhiteSpace(p.Sku) && p.Sku.Contains(skuFilter, StringComparison.OrdinalIgnoreCase))))
            .OrderBy(p => p.ProductName)
            .ToList();

        FillGrid(filtered);
    }

    private void FillGrid(List<Product> products)
    {
        _grid.Rows.Clear();

        foreach (var product in products)
        {
            _grid.Rows.Add(
                product.ProductName ?? string.Empty,
                product.Sku ?? string.Empty,
                product.SitePrice.ToString(CultureInfo.InvariantCulture),
                product.InventoryQuantity.ToString(CultureInfo.InvariantCulture),
                product.AvailabilityText,
                product.Bvin ?? string.Empty,
                product.InventoryBvin ?? string.Empty);
        }
    }

    private void GridOnCellValueChanged(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0)
            return;

        var row = _grid.Rows[e.RowIndex];
        UpdateDerivedState(row);
    }

    private void UpdateDerivedState(DataGridViewRow row)
    {
        var quantity = ParseInt(row.Cells["InventoryQuantity"].Value);
        row.Cells["AvailabilityText"].Value = quantity > 0 ? "El\u00e9rhet\u0151" : "Nem el\u00e9rhet\u0151";
    }

    private async Task SaveSelectedRowAsync()
    {
        _grid.EndEdit();

        if (_grid.CurrentRow is null)
        {
            MessageBox.Show("V\u00e1lassz ki egy term\u00e9ket.", "Ment\u00e9s", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var row = _grid.CurrentRow;
        var bvin = row.Cells["Bvin"].Value?.ToString();
        if (string.IsNullOrWhiteSpace(bvin))
        {
            MessageBox.Show("A kiv\u00e1lasztott term\u00e9khez nincs BVIN.", "Ment\u00e9s", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        var product = _allProducts.FirstOrDefault(x => string.Equals(x.Bvin, bvin, StringComparison.OrdinalIgnoreCase));
        if (product is null)
        {
            MessageBox.Show("A kiv\u00e1lasztott term\u00e9k nem tal\u00e1lhat\u00f3 a mem\u00f3ri\u00e1ban.", "Ment\u00e9s", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (!decimal.TryParse(row.Cells["SitePrice"].Value?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var newPrice))
        {
            MessageBox.Show("Az \u00e1r nem megfelel\u0151 sz\u00e1mform\u00e1tum.", "Ment\u00e9s", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (!int.TryParse(row.Cells["InventoryQuantity"].Value?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var newQuantity))
        {
            MessageBox.Show("A rakt\u00e1rk\u00e9szlet nem megfelel\u0151 eg\u00e9sz sz\u00e1m.", "Ment\u00e9s", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (newQuantity < 0)
        {
            MessageBox.Show("A rakt\u00e1rk\u00e9szlet nem lehet negat\u00edv.", "Ment\u00e9s", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        product.SitePrice = newPrice;
        product.InventoryQuantity = newQuantity;
        product.InventoryBvin = row.Cells["InventoryBvin"].Value?.ToString();
        product.Status = newQuantity > 0 ? 1 : 0;
        product.IsAvailableForSale = newQuantity > 0;

        row.Cells["AvailabilityText"].Value = product.AvailabilityText;

        if (SaveRequested is not null)
        {
            await SaveRequested.Invoke(product);
        }
    }

    private static int ParseInt(object? value)
    {
        return int.TryParse(value?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0;
    }
}