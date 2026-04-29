using DiamondzWinForms.Models;

namespace DiamondzWinForms.Controls;

public class OrdersControl : UserControl
{
    private static readonly Color ActiveStatusColor = Color.FromArgb(233, 245, 234);
    private static readonly Color ActiveStatusTextColor = Color.FromArgb(72, 120, 80);
    private static readonly Color PendingStatusColor = Color.FromArgb(232, 240, 254);
    private static readonly Color PendingStatusTextColor = Color.FromArgb(31, 88, 235);
    private static readonly Color CompletedStatusColor = Color.FromArgb(239, 241, 245);
    private static readonly Color CompletedStatusTextColor = Color.FromArgb(92, 102, 120);
    private static readonly Color HeaderColor = Color.FromArgb(34, 41, 74);
    private static readonly Color HoverRowColor = Color.FromArgb(248, 250, 255);

    private readonly Label _titleLabel;
    private readonly Panel _filterPanel;
    private readonly Label _filterHeaderLabel;
    private readonly Button _allFilterButton;
    private readonly Button _activeFilterButton;
    private readonly Button _thisWeekFilterButton;
    private readonly Button _expiredFilterButton;
    private readonly Button _pendingFilterButton;
    private readonly DataGridView _grid;
    private readonly Label _emptyStateLabel;
    private readonly Panel _detailsPanel;
    private readonly Label _detailsTitleLabel;
    private readonly TableLayoutPanel _detailsTable;
    private readonly Button _copyProductNameButton;
    private readonly Button _copySkuButton;
    private readonly Dictionary<string, Label> _detailValues = new();
    private List<Order> _orders = new();
    private List<Order> _filteredOrders = new();
    private OrderDateFilter _dateFilter = OrderDateFilter.All;
    private int _hoverRowIndex = -1;
    private string _sortColumn = "RentalStartText";
    private SortOrder _sortOrder = SortOrder.Ascending;

    public OrdersControl()
    {
        Dock = DockStyle.Fill;
        BackColor = Color.FromArgb(246, 243, 239);

        _titleLabel = new Label
        {
            Text = "K\u00f6lcs\u00f6nz\u00e9sek",
            Font = new Font("Segoe UI", 20, FontStyle.Bold),
            ForeColor = Color.FromArgb(34, 41, 74),
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter
        };

        _filterPanel = new Panel
        {
            BackColor = Color.FromArgb(232, 225, 214),
            BorderStyle = BorderStyle.None,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        _filterPanel.Paint += (_, e) =>
        {
            ControlPaint.DrawBorder(e.Graphics, _filterPanel.ClientRectangle, Color.FromArgb(178, 164, 145), ButtonBorderStyle.Solid);
        };

        _filterHeaderLabel = new Label
        {
            Text = "Dátum szűrők",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            BackColor = HeaderColor,
            ForeColor = Color.White,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter
        };

        _allFilterButton = CreateFilterButton("Összes", OrderDateFilter.All);
        _activeFilterButton = CreateFilterButton("Aktív", OrderDateFilter.Active);
        _thisWeekFilterButton = CreateFilterButton("Ezen a héten", OrderDateFilter.ThisWeek);
        _expiredFilterButton = CreateFilterButton("Lejárt", OrderDateFilter.Expired);
        _pendingFilterButton = CreateFilterButton("Függőben", OrderDateFilter.Pending);
        _filterPanel.Controls.AddRange(new Control[]
        {
            _filterHeaderLabel, _allFilterButton, _activeFilterButton, _thisWeekFilterButton, _expiredFilterButton, _pendingFilterButton
        });

        _grid = new DataGridView
        {
            Location = new Point(30, 80),
            Size = new Size(980, 520),
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AutoGenerateColumns = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            RowHeadersVisible = false,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
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
        _grid.SelectionChanged += (_, _) => ShowSelectedOrderDetails();
        _grid.CellMouseEnter += (_, e) => SetHoverRow(e.RowIndex);
        _grid.CellMouseLeave += (_, e) =>
        {
            if (e.RowIndex == _hoverRowIndex)
                SetHoverRow(-1);
        };

        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "DisplayEmail", HeaderText = "V\u00e1s\u00e1rl\u00f3 email", DataPropertyName = "DisplayEmail", FillWeight = 150 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Sku", HeaderText = "SKU", DataPropertyName = "Sku", FillWeight = 160 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "RentalStartText", HeaderText = "Kezdete", DataPropertyName = "RentalStartText", FillWeight = 110 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "RentalEndText", HeaderText = "Vége", DataPropertyName = "RentalEndText", FillWeight = 110 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ApiStatusDisplay", HeaderText = "Státusz", DataPropertyName = "ApiStatusDisplay", FillWeight = 70 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "CurrentRentalStatus", HeaderText = "Állapot", DataPropertyName = "CurrentRentalStatus", FillWeight = 150 });

        _emptyStateLabel = new Label
        {
            Text = "Nincs ilyen kölcsönzés.",
            Font = new Font("Segoe UI", 13, FontStyle.Bold),
            ForeColor = Color.FromArgb(92, 102, 120),
            BackColor = Color.White,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Visible = false
        };

        _detailsPanel = new Panel
        {
            BackColor = Color.White,
            BorderStyle = BorderStyle.None
        };
        _detailsPanel.Paint += (_, e) =>
        {
            ControlPaint.DrawBorder(e.Graphics, _detailsPanel.ClientRectangle, Color.FromArgb(226, 221, 212), ButtonBorderStyle.Solid);
        };

        _detailsTitleLabel = new Label
        {
            Text = "Kiválasztott kölcsönzés adatai",
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            BackColor = HeaderColor,
            ForeColor = Color.White,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter
        };

        _copyProductNameButton = CreateCopyButton("Név másolása");
        _copyProductNameButton.Click += (_, _) => CopyDetailValue("ProductName");

        _copySkuButton = CreateCopyButton("SKU másolása");
        _copySkuButton.Click += (_, _) => CopyDetailValue("Sku");

        _detailsTable = new TableLayoutPanel
        {
            ColumnCount = 4,
            RowCount = 4,
            BackColor = Color.White
        };
        _detailsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16));
        _detailsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
        _detailsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14));
        _detailsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));

        AddDetailRow(0, "Vásárló", "CustomerName", "Email", "DisplayEmail");
        AddDetailRow(1, "Termék neve", "ProductName", "SKU", "Sku");
        AddDetailRow(2, "Napi díj", "DailyPrice", "Teljes díj", "TotalPrice");
        AddDetailRow(3, "Hány napra", "RentalDays", "Státusz", "ApiStatusDisplay");

        Controls.Add(_titleLabel);
        Controls.Add(_filterPanel);
        Controls.Add(_grid);
        Controls.Add(_emptyStateLabel);
        Controls.Add(_detailsPanel);
        _detailsPanel.Controls.Add(_detailsTitleLabel);
        _detailsPanel.Controls.Add(_copyProductNameButton);
        _detailsPanel.Controls.Add(_copySkuButton);
        _detailsPanel.Controls.Add(_detailsTable);

        UpdateDateFilterButtons();
        Resize += (_, _) => LayoutResponsive();
        LayoutResponsive();
    }

    private void LayoutResponsive()
    {
        var left = 30;
        var width = Math.Max(900, ClientSize.Width - (left * 2));
        _titleLabel.Location = new Point(left, 20);
        _titleLabel.Size = new Size(width, 44);
        _filterPanel.Location = new Point(left, 78);
        _filterPanel.Size = new Size(width, 82);
        _filterHeaderLabel.Location = new Point(0, 0);
        _filterHeaderLabel.Size = new Size(width, 32);

        var buttonLeft = 16;
        var buttonTop = 42;
        var buttonGap = 10;
        foreach (var button in new[] { _allFilterButton, _activeFilterButton, _thisWeekFilterButton, _expiredFilterButton, _pendingFilterButton })
        {
            button.Location = new Point(buttonLeft, buttonTop);
            buttonLeft = button.Right + buttonGap;
        }

        _grid.Location = new Point(left, _filterPanel.Bottom + 12);
        _grid.Size = new Size(width, Math.Max(260, ClientSize.Height - _grid.Top - 220));
        _emptyStateLabel.Location = _grid.Location;
        _emptyStateLabel.Size = _grid.Size;
        _emptyStateLabel.BringToFront();
        _detailsPanel.Location = new Point(left, _grid.Bottom + 16);
        _detailsPanel.Size = new Size(width, Math.Max(160, ClientSize.Height - _grid.Bottom - 36));
        _detailsTitleLabel.Location = new Point(0, 0);
        _detailsTitleLabel.Size = new Size(width, 34);
        _copySkuButton.Location = new Point(_detailsPanel.Width - _copySkuButton.Width - 18, 4);
        _copyProductNameButton.Location = new Point(_copySkuButton.Left - _copyProductNameButton.Width - 10, 4);
        _copySkuButton.BringToFront();
        _copyProductNameButton.BringToFront();
        _detailsTable.ColumnStyles.Clear();
        _detailsTable.ColumnCount = 4;
        _detailsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16));
        _detailsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
        _detailsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14));
        _detailsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
        _detailsTable.Location = new Point(24, 48);
        _detailsTable.Size = new Size(width - 48, Math.Max(90, _detailsPanel.Height - 60));
    }

    public void BindData(List<Order> orders)
    {
        BindData(orders, null);
    }

    public void BindData(List<Order> orders, List<Product>? products)
    {
        _orders = orders ?? new List<Order>();

        var productsBySku = (products ?? new List<Product>())
            .Where(x => !string.IsNullOrWhiteSpace(x.Sku))
            .GroupBy(x => x.Sku!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First().ProductName ?? string.Empty, StringComparer.OrdinalIgnoreCase);

        foreach (var order in _orders)
        {
            order.ProductName = !string.IsNullOrWhiteSpace(order.Sku) && productsBySku.TryGetValue(order.Sku, out var productName)
                ? productName
                : string.Empty;
        }

        RebindOrdersGrid();
        ShowSelectedOrderDetails();
    }

    public void ShowAllOrders()
    {
        SetDateFilter(OrderDateFilter.All);
    }

    public void ShowPaidOrders()
    {
        SetDateFilter(OrderDateFilter.Paid);
    }

    public void ShowActiveOrders()
    {
        SetDateFilter(OrderDateFilter.Active);
    }

    public void ShowThisWeekOrders()
    {
        SetDateFilter(OrderDateFilter.ThisWeek);
    }

    public void ShowExpiredOrders()
    {
        SetDateFilter(OrderDateFilter.Expired);
    }

    public void ShowPendingOrders()
    {
        SetDateFilter(OrderDateFilter.Pending);
    }

    private void GridOnCellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0)
            return;

        var columnName = _grid.Columns[e.ColumnIndex].Name;
        if (!string.Equals(columnName, "CurrentRentalStatus", StringComparison.Ordinal) &&
            !string.Equals(columnName, "ApiStatusDisplay", StringComparison.Ordinal))
            return;

        var status = e.Value?.ToString();
        var cell = _grid.Rows[e.RowIndex].Cells[e.ColumnIndex];

        if (string.Equals(status, "Aktív", StringComparison.OrdinalIgnoreCase))
        {
            cell.Style.BackColor = ActiveStatusColor;
            cell.Style.ForeColor = ActiveStatusTextColor;
        }
        else if (string.Equals(status, "Függőben", StringComparison.OrdinalIgnoreCase))
        {
            cell.Style.BackColor = PendingStatusColor;
            cell.Style.ForeColor = PendingStatusTextColor;
        }
        else if (string.Equals(status, "Befejezett", StringComparison.OrdinalIgnoreCase))
        {
            cell.Style.BackColor = CompletedStatusColor;
            cell.Style.ForeColor = CompletedStatusTextColor;
        }
        else if (string.Equals(status, "Kosárba rakva, rendelésre vár", StringComparison.OrdinalIgnoreCase))
        {
            cell.Style.BackColor = Color.FromArgb(255, 243, 205);
            cell.Style.ForeColor = Color.FromArgb(133, 100, 4);
        }
        else if (string.Equals(status, "Paid", StringComparison.OrdinalIgnoreCase))
        {
            cell.Style.BackColor = ActiveStatusColor;
            cell.Style.ForeColor = ActiveStatusTextColor;
        }
        else if (string.Equals(status, "Draft", StringComparison.OrdinalIgnoreCase))
        {
            cell.Style.BackColor = Color.FromArgb(255, 243, 205);
            cell.Style.ForeColor = Color.FromArgb(133, 100, 4);
        }
    }

    private void GridOnColumnHeaderMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
    {
        if (e.ColumnIndex < 0)
            return;

        var column = _grid.Columns[e.ColumnIndex];
        if (string.Equals(_sortColumn, column.Name, StringComparison.Ordinal))
        {
            _sortOrder = _sortOrder == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending;
        }
        else
        {
            _sortColumn = column.Name;
            _sortOrder = SortOrder.Ascending;
        }

        RebindOrdersGrid();
    }

    private void SetDateFilter(OrderDateFilter filter)
    {
        _dateFilter = filter;
        UpdateDateFilterButtons();
        RebindOrdersGrid();
        ShowSelectedOrderDetails();
    }

    private void UpdateDateFilterButtons()
    {
        UpdateDateFilterButton(_allFilterButton, OrderDateFilter.All);
        UpdateDateFilterButton(_activeFilterButton, OrderDateFilter.Active);
        UpdateDateFilterButton(_thisWeekFilterButton, OrderDateFilter.ThisWeek);
        UpdateDateFilterButton(_expiredFilterButton, OrderDateFilter.Expired);
        UpdateDateFilterButton(_pendingFilterButton, OrderDateFilter.Pending);
    }

    private void UpdateDateFilterButton(Button button, OrderDateFilter filter)
    {
        var selected = _dateFilter == filter;
        button.BackColor = selected ? HeaderColor : Color.FromArgb(120, 128, 160);
        button.ForeColor = Color.White;
        button.FlatAppearance.MouseOverBackColor = selected ? Color.FromArgb(48, 58, 100) : Color.FromArgb(144, 151, 180);
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

    private void AddDetailRow(int rowIndex, string leftTitle, string leftKey, string rightTitle, string rightKey)
    {
        _detailsTable.RowStyles.Add(new RowStyle(SizeType.Percent, 25));

        var leftLabel = CreateDetailTitleLabel(leftTitle);
        var leftValue = CreateDetailValueLabel();
        var rightLabel = CreateDetailTitleLabel(rightTitle);
        var rightValue = CreateDetailValueLabel();

        _detailsTable.Controls.Add(leftLabel, 0, rowIndex);
        _detailsTable.Controls.Add(leftValue, 1, rowIndex);
        _detailsTable.Controls.Add(rightLabel, 2, rowIndex);
        _detailsTable.Controls.Add(rightValue, 3, rowIndex);

        if (string.IsNullOrWhiteSpace(rightTitle))
        {
            rightLabel.Visible = false;
            rightValue.Visible = false;
        }

        _detailValues[leftKey] = leftValue;
        _detailValues[rightKey] = rightValue;
    }

    private static Label CreateDetailTitleLabel(string text)
    {
        return new Label
        {
            Text = string.IsNullOrWhiteSpace(text) ? string.Empty : text + ":",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.FromArgb(34, 41, 74),
            AutoSize = false,
            AutoEllipsis = true,
            TextAlign = ContentAlignment.MiddleLeft,
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 4, 8, 4)
        };
    }

    private static Label CreateDetailValueLabel()
    {
        return new Label
        {
            Text = string.Empty,
            Font = new Font("Segoe UI", 10, FontStyle.Regular),
            ForeColor = Color.FromArgb(60, 60, 60),
            AutoSize = false,
            AutoEllipsis = true,
            TextAlign = ContentAlignment.MiddleLeft,
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 4, 8, 4)
        };
    }

    private void ShowSelectedOrderDetails()
    {
        var order = _grid.CurrentRow?.DataBoundItem as Order ?? _filteredOrders.FirstOrDefault();
        if (order is null)
        {
            foreach (var valueLabel in _detailValues.Values)
            {
                valueLabel.Text = string.Empty;
            }

            return;
        }

        SetDetailValue("CustomerName", order.CustomerName);
        SetDetailValue("DisplayEmail", order.DisplayEmail);
        SetDetailValue("ProductName", string.IsNullOrWhiteSpace(order.ProductName) ? "Nincs terméknév" : order.ProductName);
        SetDetailValue("Sku", order.Sku);
        SetDetailValue("DailyPrice", FormatMoney(order.DailyPrice));
        SetDetailValue("TotalPrice", FormatMoney(order.TotalPrice > 0 ? order.TotalPrice : order.TotalGrand));
        SetDetailValue("RentalDays", order.RentalDays > 0 ? $"{order.RentalDays} nap" : string.Empty);
        SetDetailValue("ApiStatusDisplay", order.ApiStatusDisplay);
    }

    private void SetDetailValue(string key, string? value)
    {
        if (_detailValues.TryGetValue(key, out var label))
        {
            label.Text = string.IsNullOrWhiteSpace(value) ? string.Empty : value;
        }
    }

    private static string FormatMoney(decimal amount)
    {
        return amount > 0 ? $"{amount:N0} Ft" : string.Empty;
    }

    private static Button CreateCopyButton(string text)
    {
        var button = new Button
        {
            Text = text,
            Size = new Size(118, 28),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(202, 162, 107),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9, FontStyle.Bold)
        };
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(220, 180, 124);
        return button;
    }

    private Button CreateFilterButton(string text, OrderDateFilter filter)
    {
        var button = new Button
        {
            Text = text,
            Size = new Size(text.Length > 10 ? 130 : 96, 30),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(120, 128, 160),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9, FontStyle.Bold)
        };
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(144, 151, 180);
        button.Click += (_, _) => SetDateFilter(filter);
        return button;
    }

    private void CopyDetailValue(string key)
    {
        if (_detailValues.TryGetValue(key, out var label) && !string.IsNullOrWhiteSpace(label.Text))
        {
            Clipboard.SetText(label.Text);
        }
    }

    private void RebindOrdersGrid()
    {
        _filteredOrders = SortOrders(FilterOrders(_orders));
        _grid.DataSource = null;
        _grid.DataSource = _filteredOrders;
        _emptyStateLabel.Visible = _filteredOrders.Count == 0;
        if (_emptyStateLabel.Visible)
            _emptyStateLabel.BringToFront();
        UpdateSortGlyphs();
    }

    private List<Order> FilterOrders(List<Order> orders)
    {
        var today = DateTime.Today;
        var weekStart = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
        var weekEnd = weekStart.AddDays(6);

        return orders
            .Where(order => _dateFilter switch
            {
                OrderDateFilter.Active => string.Equals(order.CurrentRentalStatus, "Aktív", StringComparison.OrdinalIgnoreCase),
                OrderDateFilter.ThisWeek => DateIntersects(order, weekStart, weekEnd),
                OrderDateFilter.Expired => IsExpired(order, today),
                OrderDateFilter.Pending => string.Equals(order.CurrentRentalStatus, "Függőben", StringComparison.OrdinalIgnoreCase),
                OrderDateFilter.Paid => IsPaid(order),
                _ => true
            })
            .ToList();
    }

    private static bool IsPaid(Order order)
    {
        return string.Equals(order.Status, "Paid", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(order.StatusName, "Paid", StringComparison.OrdinalIgnoreCase);
    }

    private static bool DateIntersects(Order order, DateTime start, DateTime end)
    {
        var rentalStart = order.RentalStart?.Date;
        var rentalEnd = order.RentalEnd?.Date ?? rentalStart;

        if (!rentalStart.HasValue || !rentalEnd.HasValue)
            return false;

        return rentalStart.Value <= end.Date && rentalEnd.Value >= start.Date;
    }

    private static bool IsExpired(Order order, DateTime today)
    {
        return string.Equals(order.CurrentRentalStatus, "Befejezett", StringComparison.OrdinalIgnoreCase) ||
               (order.RentalEnd.HasValue && order.RentalEnd.Value.Date < today.Date);
    }

    private List<Order> SortOrders(List<Order> orders)
    {
        Func<Order, object?> keySelector = _sortColumn switch
        {
            "DisplayEmail" => o => o.DisplayEmail,
            "Sku" => o => o.Sku,
            "RentalStartText" => o => o.RentalStart,
            "RentalEndText" => o => o.RentalEnd,
            "ApiStatusDisplay" => o => o.ApiStatusDisplay,
            "CurrentRentalStatus" => o => o.CurrentRentalStatus,
            _ => o => o.RentalStart
        };

        return _sortOrder == SortOrder.Descending
            ? orders.OrderByDescending(keySelector).ThenBy(o => o.Sku).ToList()
            : orders.OrderBy(keySelector).ThenBy(o => o.Sku).ToList();
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

    private enum OrderDateFilter
    {
        All,
        Active,
        ThisWeek,
        Expired,
        Pending,
        Paid
    }
}
