using System.Globalization;
using System.Text;
using DiamondzWinForms.Models;

namespace DiamondzWinForms.Controls;

public class ProductsControl : UserControl
{
    private static readonly Color EditablePriceColor = Color.FromArgb(255, 248, 220);
    private static readonly Color EditableStockColor = Color.FromArgb(232, 242, 255);
    private static readonly Color ZeroStockColor = Color.FromArgb(220, 53, 69);
    private static readonly Color LowStockColor = Color.FromArgb(255, 243, 205);
    private static readonly Color LowStockTextColor = Color.FromArgb(133, 100, 4);
    private static readonly Color GoodStockColor = Color.FromArgb(233, 245, 234);
    private static readonly Color GoodStockTextColor = Color.FromArgb(72, 120, 80);
    private static readonly Color AvailableColor = Color.FromArgb(233, 245, 234);
    private static readonly Color AvailableTextColor = Color.FromArgb(72, 120, 80);
    private static readonly Color UnavailableColor = Color.FromArgb(253, 237, 237);
    private static readonly Color UnavailableTextColor = Color.FromArgb(176, 42, 55);
    private static readonly Color HeaderColor = Color.FromArgb(34, 41, 74);
    private static readonly Color HoverRowColor = Color.FromArgb(248, 250, 255);

    private readonly Label _titleLabel;
    private readonly Panel _filterPanel;
    private readonly Label _filterHeaderLabel;
    private readonly Label _nameLabel;
    private readonly TextBox _nameTextBox;
    private readonly Label _skuLabel;
    private readonly TextBox _skuTextBox;
    private readonly Button _clearButton;
    private readonly Button _rentableOnlyButton;
    private readonly Button _purchasableOnlyButton;
    private readonly Button _unavailableOnlyButton;
    private readonly Button _exportButton;
    private readonly Button _saveButton;
    private readonly DataGridView _grid;
    private readonly Label _emptyStateLabel;

    private List<Product> _allProducts = new();
    private List<Product> _filteredProducts = new();
    private bool _showUnavailableOnly;
    private bool _showAvailableOnly;
    private bool _showLowStockOnly;
    private int _hoverRowIndex = -1;
    private ProductQuickFilter _quickFilter = ProductQuickFilter.All;
    private string _sortColumn = "ProductName";
    private SortOrder _sortOrder = SortOrder.Ascending;

    public event Func<Product, Task>? SaveRequested;

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
            BackColor = Color.FromArgb(232, 225, 214),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        _filterPanel.Paint += (_, e) =>
        {
            ControlPaint.DrawBorder(e.Graphics, _filterPanel.ClientRectangle, Color.FromArgb(178, 164, 145), ButtonBorderStyle.Solid);
        };

        _filterHeaderLabel = new Label
        {
            Text = "Szűrők és műveletek",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            BackColor = HeaderColor,
            ForeColor = Color.White,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter
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
            _showUnavailableOnly = false;
            _showAvailableOnly = false;
            _showLowStockOnly = false;
            _quickFilter = ProductQuickFilter.All;
            _sortColumn = "ProductName";
            _sortOrder = SortOrder.Ascending;
            UpdateQuickFilterButtonStates();
            UpdateUnavailableOnlyButtonState();
            ApplyFilters();
        };

        _rentableOnlyButton = CreateActionButton("Csak bérelhető", 130, Color.FromArgb(120, 128, 160));
        _rentableOnlyButton.Click += (_, _) =>
        {
            _showLowStockOnly = false;
            _showAvailableOnly = false;
            _quickFilter = _quickFilter == ProductQuickFilter.Rentable ? ProductQuickFilter.All : ProductQuickFilter.Rentable;
            UpdateQuickFilterButtonStates();
            ApplyFilters();
        };

        _purchasableOnlyButton = CreateActionButton("Csak megvásárolható", 170, Color.FromArgb(120, 128, 160));
        _purchasableOnlyButton.Click += (_, _) =>
        {
            _showLowStockOnly = false;
            _showAvailableOnly = false;
            _quickFilter = _quickFilter == ProductQuickFilter.Purchasable ? ProductQuickFilter.All : ProductQuickFilter.Purchasable;
            UpdateQuickFilterButtonStates();
            ApplyFilters();
        };

        _unavailableOnlyButton = CreateActionButton("Nem elérhető termékek", 190, Color.FromArgb(120, 128, 160));
        _unavailableOnlyButton.Click += (_, _) =>
        {
            _showLowStockOnly = false;
            _showAvailableOnly = false;
            _showUnavailableOnly = !_showUnavailableOnly;
            UpdateUnavailableOnlyButtonState();
            ApplyFilters();
        };

        _exportButton = CreateActionButton("Export CSV", 110, Color.FromArgb(66, 133, 244));
        _exportButton.Click += (_, _) => ExportFilteredProducts();

        UpdateQuickFilterButtonStates();
        UpdateUnavailableOnlyButtonState();

        _saveButton = CreateActionButton("Ment\u00e9s a kijel\u00f6lt sorra", 190, Color.FromArgb(34, 41, 74));
        _saveButton.Click += async (_, _) => await SaveSelectedRowAsync();

        _filterPanel.Controls.AddRange(new Control[]
        {
            _filterHeaderLabel, _nameLabel, _nameTextBox, _skuLabel, _skuTextBox, _clearButton, _rentableOnlyButton, _purchasableOnlyButton, _unavailableOnlyButton, _exportButton, _saveButton
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
        _grid.ColumnHeadersDefaultCellStyle.BackColor = HeaderColor;
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        _grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = HeaderColor;
        _grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
        _grid.DefaultCellStyle.Font = new Font("Segoe UI", 10);
        _grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(232, 240, 255);
        _grid.DefaultCellStyle.SelectionForeColor = Color.Black;
        _grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 250, 248);
        _grid.EnableHeadersVisualStyles = false;
        _grid.ColumnHeaderMouseClick += GridOnColumnHeaderMouseClick;
        _grid.CellFormatting += GridOnCellFormatting;
        _grid.CellValueChanged += GridOnCellValueChanged;
        _grid.CellMouseEnter += (_, e) => SetHoverRow(e.RowIndex);
        _grid.CellMouseLeave += (_, e) =>
        {
            if (e.RowIndex == _hoverRowIndex)
                SetHoverRow(-1);
        };

        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "N\u00e9v", DataPropertyName = "ProductName", ReadOnly = true, MinimumWidth = 320 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Sku", HeaderText = "SKU", DataPropertyName = "Sku", ReadOnly = true, MinimumWidth = 240 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "SitePrice", HeaderText = "\u00c1r", ReadOnly = false, MinimumWidth = 170, DefaultCellStyle = new DataGridViewCellStyle { BackColor = EditablePriceColor } });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "InventoryQuantity", HeaderText = "Rakt\u00e1ron", ReadOnly = false, MinimumWidth = 110, DefaultCellStyle = new DataGridViewCellStyle { BackColor = EditableStockColor } });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "AvailabilityText", HeaderText = "\u00c1llapot", ReadOnly = true, MinimumWidth = 150 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Bvin", HeaderText = "Bvin", ReadOnly = true, MinimumWidth = 340 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "InventoryBvin", HeaderText = "InventoryBvin", Visible = false, ReadOnly = true });

        _emptyStateLabel = new Label
        {
            Text = "Nincs ilyen termék.",
            Font = new Font("Segoe UI", 13, FontStyle.Bold),
            ForeColor = Color.FromArgb(92, 102, 120),
            BackColor = Color.White,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Visible = false
        };

        Controls.Add(_titleLabel);
        Controls.Add(_filterPanel);
        Controls.Add(_grid);
        Controls.Add(_emptyStateLabel);

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
        button.FlatAppearance.MouseOverBackColor = Lighten(backColor);
        return button;
    }

    private static Color Lighten(Color color)
    {
        return Color.FromArgb(
            Math.Min(255, color.R + 24),
            Math.Min(255, color.G + 24),
            Math.Min(255, color.B + 24));
    }

    public void BindData(List<Product> products)
    {
        _allProducts = products ?? new List<Product>();
        ApplyFilters();
    }

    public void ShowAllProducts()
    {
        ApplyDashboardFilter(ProductQuickFilter.All, showAvailableOnly: false, showUnavailableOnly: false, showLowStockOnly: false, "ProductName", SortOrder.Ascending);
    }

    public void ShowAvailableProducts()
    {
        ApplyDashboardFilter(ProductQuickFilter.All, showAvailableOnly: true, showUnavailableOnly: false, showLowStockOnly: false, "ProductName", SortOrder.Ascending);
    }

    public void ShowPurchasableProducts()
    {
        ApplyDashboardFilter(ProductQuickFilter.Purchasable, showAvailableOnly: false, showUnavailableOnly: false, showLowStockOnly: false, "ProductName", SortOrder.Ascending);
    }

    public void ShowRentableProducts()
    {
        ApplyDashboardFilter(ProductQuickFilter.Rentable, showAvailableOnly: false, showUnavailableOnly: false, showLowStockOnly: false, "ProductName", SortOrder.Ascending);
    }

    public void ShowLowStockProducts()
    {
        ApplyDashboardFilter(ProductQuickFilter.Purchasable, showAvailableOnly: false, showUnavailableOnly: false, showLowStockOnly: true, "InventoryQuantity", SortOrder.Ascending);
    }

    public void ShowOutOfStockPurchasableProducts()
    {
        ApplyDashboardFilter(ProductQuickFilter.Purchasable, showAvailableOnly: false, showUnavailableOnly: true, showLowStockOnly: false, "InventoryQuantity", SortOrder.Ascending);
    }

    private void ApplyDashboardFilter(ProductQuickFilter quickFilter, bool showAvailableOnly, bool showUnavailableOnly, bool showLowStockOnly, string sortColumn, SortOrder sortOrder)
    {
        _nameTextBox.Text = string.Empty;
        _skuTextBox.Text = string.Empty;
        _showAvailableOnly = showAvailableOnly;
        _showUnavailableOnly = showUnavailableOnly;
        _showLowStockOnly = showLowStockOnly;
        _quickFilter = quickFilter;
        _sortColumn = sortColumn;
        _sortOrder = sortOrder;
        UpdateQuickFilterButtonStates();
        UpdateUnavailableOnlyButtonState();
        ApplyFilters();
    }

    private void LayoutResponsive()
    {
        var left = 24;
        var gap = 12;
        var contentWidth = Math.Max(720, ClientSize.Width - (left * 2));

        _titleLabel.Location = new Point(left, 20);
        _titleLabel.Size = new Size(contentWidth, 44);

        _filterPanel.Location = new Point(left, 78);
        _filterPanel.Size = new Size(contentWidth, 126);
        _filterHeaderLabel.Location = new Point(0, 0);
        _filterHeaderLabel.Size = new Size(contentWidth, 32);

        _nameLabel.Location = new Point(16, 48);
        _nameTextBox.Location = new Point(60, 44);
        var firstRowGap = 24;
        var labelWidth = 44;
        var skuLabelWidth = 46;
        var remainingWidth = Math.Max(360, _filterPanel.Width - labelWidth - skuLabelWidth - firstRowGap - 24);
        var halfWidth = remainingWidth / 2;
        _nameTextBox.Size = new Size(Math.Max(180, halfWidth - 12), 27);
        _skuLabel.Location = new Point(_nameTextBox.Right + firstRowGap, 48);
        _skuTextBox.Location = new Point(_skuLabel.Right + 8, 44);
        _skuTextBox.Size = new Size(Math.Max(180, _filterPanel.Width - _skuTextBox.Left - 16), 27);

        var buttonY = 82;
        _clearButton.Location = new Point(_nameTextBox.Left, buttonY);
        _rentableOnlyButton.Location = new Point(_clearButton.Right + gap, buttonY);
        _purchasableOnlyButton.Location = new Point(_rentableOnlyButton.Right + gap, buttonY);
        _unavailableOnlyButton.Location = new Point(_purchasableOnlyButton.Right + gap, buttonY);
        _exportButton.Location = new Point(_unavailableOnlyButton.Right + gap, buttonY);
        _saveButton.Location = new Point(_exportButton.Right + gap, buttonY);

        var filterBottom = _filterPanel.Bottom;
        _grid.Location = new Point(left, filterBottom + 12);
        _grid.Size = new Size(contentWidth, Math.Max(340, ClientSize.Height - _grid.Top - 24));
        _emptyStateLabel.Location = _grid.Location;
        _emptyStateLabel.Size = _grid.Size;
        _emptyStateLabel.BringToFront();

        LayoutGridColumns();
    }

    private void LayoutGridColumns()
    {
        var availableWidth = Math.Max(720, _grid.ClientSize.Width - 4);
        var compact = availableWidth < 1100;
        var hideBvin = availableWidth < 900;
        var nameWidth = compact ? 260 : 360;
        var skuWidth = compact ? 200 : 280;
        var priceWidth = compact ? 120 : 170;
        var qtyWidth = 110;
        var availabilityWidth = compact ? 120 : 150;
        var reservedWidth = nameWidth + skuWidth + priceWidth + qtyWidth + availabilityWidth;
        var bvinWidth = hideBvin ? 0 : Math.Max(240, availableWidth - reservedWidth);

        _grid.Columns["ProductName"].Width = nameWidth;
        _grid.Columns["Sku"].Width = skuWidth;
        _grid.Columns["SitePrice"].Width = priceWidth;
        _grid.Columns["InventoryQuantity"].Width = qtyWidth;
        _grid.Columns["AvailabilityText"].Width = availabilityWidth;
        _grid.Columns["Bvin"].Visible = !hideBvin;
        if (!hideBvin)
            _grid.Columns["Bvin"].Width = bvinWidth;
    }

    private void ApplyFilters()
    {
        var nameFilter = _nameTextBox.Text.Trim();
        var skuFilter = _skuTextBox.Text.Trim();

        var filtered = _allProducts
            .Where(p =>
                (string.IsNullOrWhiteSpace(nameFilter) || (!string.IsNullOrWhiteSpace(p.ProductName) && p.ProductName.Contains(nameFilter, StringComparison.OrdinalIgnoreCase))) &&
                (string.IsNullOrWhiteSpace(skuFilter) || (!string.IsNullOrWhiteSpace(p.Sku) && p.Sku.Contains(skuFilter, StringComparison.OrdinalIgnoreCase))) &&
                (_quickFilter != ProductQuickFilter.Rentable || p.IsRentableProduct) &&
                (_quickFilter != ProductQuickFilter.Purchasable || !p.IsRentableProduct) &&
                (!_showAvailableOnly || p.EffectiveIsAvailable) &&
                (!_showUnavailableOnly || !p.EffectiveIsAvailable) &&
                (!_showLowStockOnly || (!p.IsRentableProduct && p.InventoryQuantity <= 2)))
            .ToList();

        _filteredProducts = SortProducts(filtered);
        FillGrid(_filteredProducts);
    }

    private void FillGrid(List<Product> products)
    {
        _grid.Rows.Clear();
        UpdateSortGlyphs();

        foreach (var product in products)
        {
            var rowIndex = _grid.Rows.Add(
                product.ProductName ?? string.Empty,
                product.Sku ?? string.Empty,
                product.SitePrice.ToString(CultureInfo.InvariantCulture),
                product.InventoryQuantity.ToString(CultureInfo.InvariantCulture),
                product.AvailabilityText,
                product.Bvin ?? string.Empty,
                product.InventoryBvin ?? string.Empty);

            UpdateDerivedState(_grid.Rows[rowIndex]);
        }

        _emptyStateLabel.Visible = products.Count == 0;
        if (_emptyStateLabel.Visible)
            _emptyStateLabel.BringToFront();
    }

    private void GridOnCellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0)
            return;

        var columnName = _grid.Columns[e.ColumnIndex].Name;
        var cell = _grid.Rows[e.RowIndex].Cells[e.ColumnIndex];
        if (string.Equals(columnName, "InventoryQuantity", StringComparison.Ordinal))
        {
            var quantity = ParseInt(cell.Value);

            if (quantity == 0)
            {
                cell.Style.BackColor = ZeroStockColor;
                cell.Style.ForeColor = Color.White;
                cell.Style.SelectionBackColor = Color.FromArgb(176, 42, 55);
                cell.Style.SelectionForeColor = Color.White;
            }
            else if (quantity <= 2)
            {
                cell.Style.BackColor = LowStockColor;
                cell.Style.ForeColor = LowStockTextColor;
                cell.Style.SelectionBackColor = Color.FromArgb(202, 162, 107);
                cell.Style.SelectionForeColor = Color.White;
            }
            else
            {
                cell.Style.BackColor = GoodStockColor;
                cell.Style.ForeColor = GoodStockTextColor;
                cell.Style.SelectionBackColor = Color.FromArgb(72, 120, 80);
                cell.Style.SelectionForeColor = Color.White;
            }

            return;
        }

        if (string.Equals(columnName, "AvailabilityText", StringComparison.Ordinal))
        {
            var status = cell.Value?.ToString();

            if (string.Equals(status, "Elérhető", StringComparison.OrdinalIgnoreCase))
            {
                cell.Style.BackColor = AvailableColor;
                cell.Style.ForeColor = AvailableTextColor;
            }
            else if (string.Equals(status, "Nem elérhető", StringComparison.OrdinalIgnoreCase))
            {
                cell.Style.BackColor = UnavailableColor;
                cell.Style.ForeColor = UnavailableTextColor;
            }

            cell.Style.SelectionBackColor = SystemColors.Highlight;
            cell.Style.SelectionForeColor = SystemColors.HighlightText;
        }
    }

    private void GridOnCellValueChanged(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0)
            return;

        var row = _grid.Rows[e.RowIndex];
        UpdateDerivedState(row);
        _grid.InvalidateRow(e.RowIndex);
    }

    private void SetHoverRow(int rowIndex)
    {
        if (_hoverRowIndex == rowIndex)
            return;

        if (_hoverRowIndex >= 0 && _hoverRowIndex < _grid.Rows.Count)
            _grid.Rows[_hoverRowIndex].DefaultCellStyle.BackColor = _hoverRowIndex % 2 == 0 ? Color.White : Color.FromArgb(250, 250, 248);

        _hoverRowIndex = rowIndex >= 0 && rowIndex < _grid.Rows.Count ? rowIndex : -1;

        if (_hoverRowIndex >= 0)
            _grid.Rows[_hoverRowIndex].DefaultCellStyle.BackColor = HoverRowColor;
    }

    private void GridOnColumnHeaderMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
    {
        if (e.ColumnIndex < 0)
            return;

        var column = _grid.Columns[e.ColumnIndex];
        if (!column.Visible || string.Equals(column.Name, "InventoryBvin", StringComparison.Ordinal))
            return;

        if (string.Equals(_sortColumn, column.Name, StringComparison.Ordinal))
        {
            _sortOrder = _sortOrder == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending;
        }
        else
        {
            _sortColumn = column.Name;
            _sortOrder = SortOrder.Ascending;
        }

        ApplyFilters();
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

    private void UpdateUnavailableOnlyButtonState()
    {
        var color = _showUnavailableOnly
            ? Color.FromArgb(176, 42, 55)
            : Color.FromArgb(120, 128, 160);
        _unavailableOnlyButton.BackColor = color;
        _unavailableOnlyButton.FlatAppearance.MouseOverBackColor = Lighten(color);
    }

    private void UpdateQuickFilterButtonStates()
    {
        var rentableColor = _quickFilter == ProductQuickFilter.Rentable
            ? Color.FromArgb(34, 41, 74)
            : Color.FromArgb(120, 128, 160);
        _rentableOnlyButton.BackColor = rentableColor;
        _rentableOnlyButton.FlatAppearance.MouseOverBackColor = Lighten(rentableColor);

        var purchasableColor = _quickFilter == ProductQuickFilter.Purchasable
            ? Color.FromArgb(34, 41, 74)
            : Color.FromArgb(120, 128, 160);
        _purchasableOnlyButton.BackColor = purchasableColor;
        _purchasableOnlyButton.FlatAppearance.MouseOverBackColor = Lighten(purchasableColor);
    }

    private List<Product> SortProducts(List<Product> products)
    {
        Func<Product, object?> keySelector = _sortColumn switch
        {
            "Sku" => p => p.Sku,
            "SitePrice" => p => p.SitePrice,
            "InventoryQuantity" => p => p.InventoryQuantity,
            "AvailabilityText" => p => p.AvailabilityText,
            "Bvin" => p => p.Bvin,
            _ => p => p.ProductName
        };

        return _sortOrder == SortOrder.Descending
            ? products.OrderByDescending(keySelector).ThenBy(p => p.ProductName).ToList()
            : products.OrderBy(keySelector).ThenBy(p => p.ProductName).ToList();
    }

    private void UpdateSortGlyphs()
    {
        foreach (DataGridViewColumn column in _grid.Columns)
        {
            column.HeaderCell.SortGlyphDirection = string.Equals(column.Name, _sortColumn, StringComparison.Ordinal)
                ? _sortOrder
                : SortOrder.None;
        }
    }

    private void ExportFilteredProducts()
    {
        if (_filteredProducts.Count == 0)
        {
            MessageBox.Show("Nincs exportálható termék a jelenlegi szűrőkkel.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Filter = "CSV fájl (*.csv)|*.csv",
            FileName = $"termekek_{DateTime.Now:yyyyMMdd_HHmm}.csv",
            Title = "Termékek exportálása"
        };

        if (dialog.ShowDialog() != DialogResult.OK || string.IsNullOrWhiteSpace(dialog.FileName))
            return;

        var lines = new List<string>
        {
            "Nev;SKU;Ar;Raktaron;Allapot;Tipus;Bvin"
        };

        lines.AddRange(_filteredProducts.Select(product => string.Join(";",
            EscapeCsv(product.ProductName),
            EscapeCsv(product.Sku),
            product.SitePrice.ToString(CultureInfo.InvariantCulture),
            product.InventoryQuantity.ToString(CultureInfo.InvariantCulture),
            EscapeCsv(product.AvailabilityText),
            EscapeCsv(product.IsRentableProduct ? "Bérelhető" : "Megvásárolható"),
            EscapeCsv(product.Bvin))));

        File.WriteAllText(dialog.FileName, string.Join(Environment.NewLine, lines), new UTF8Encoding(true));
        MessageBox.Show("A termékek exportálása sikeres volt.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private static string EscapeCsv(string? value)
    {
        var text = value ?? string.Empty;
        return $"\"{text.Replace("\"", "\"\"")}\"";
    }

    private enum ProductQuickFilter
    {
        All,
        Rentable,
        Purchasable
    }
}

