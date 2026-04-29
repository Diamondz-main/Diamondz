using DiamondzWinForms.Models;
using DiamondzWinForms.Helpers;

namespace DiamondzWinForms.Controls;

public class DashboardControl : UserControl
{
    private readonly Label _titleLabel;
    private readonly Panel _allProductsGroupPanel;
    private readonly Panel _purchasableGroupPanel;
    private readonly Panel _rentableGroupPanel;
    private readonly Label _allProductsSectionLabel;
    private readonly Label _purchasableSectionLabel;
    private readonly Label _rentableSectionLabel;

    private readonly Label _totalProductsValue;
    private readonly Label _availableProductsValue;
    private readonly Label _totalStockValue;

    private readonly Label _purchasableProductsValue;
    private readonly Label _lowStockValue;
    private readonly Label _outOfStockPurchasableValue;

    private readonly Label _rentableProductsValue;
    private readonly Label _paidRentalsValue;
    private readonly Label _completedRentalsValue;
    private readonly Label _pendingRentalsValue;
    private readonly Label _activeRentalsValue;

    private readonly Panel _ordersChartGroupPanel;
    private readonly Panel _purchasableStockChartGroupPanel;
    private readonly Panel _ordersChartPanel;
    private readonly Panel _purchasableStockChartPanel;

    private readonly List<Panel> _allProductsCards = new();
    private readonly List<Panel> _purchasableCards = new();
    private readonly List<Panel> _rentableCards = new();
    private readonly List<DashboardChartItem> _ordersChartItems = new();
    private readonly List<DashboardChartItem> _purchasableStockChartItems = new();
    private const string ValueLabelTag = "DashboardValue";
    private const string SubtitleLabelTag = "DashboardSubtitle";

    public event Action? AllProductsRequested;
    public event Action? AvailableProductsRequested;
    public event Action? PurchasableProductsRequested;
    public event Action? LowStockProductsRequested;
    public event Action? OutOfStockPurchasableProductsRequested;
    public event Action? RentableProductsRequested;
    public event Action? AllOrdersRequested;
    public event Action? CompletedOrdersRequested;
    public event Action? PendingOrdersRequested;
    public event Action? ActiveOrdersRequested;

    public DashboardControl()
    {
        Dock = DockStyle.Fill;
        BackColor = Color.FromArgb(246, 243, 239);

        _titleLabel = new Label
        {
            Text = "Dashboard",
            Font = new Font("Segoe UI", 20, FontStyle.Bold),
            ForeColor = Color.FromArgb(34, 41, 74),
            AutoSize = false,
            AutoEllipsis = true,
            TextAlign = ContentAlignment.MiddleCenter
        };

        _allProductsGroupPanel = CreateGroupPanel();
        _purchasableGroupPanel = CreateGroupPanel();
        _rentableGroupPanel = CreateGroupPanel();
        _ordersChartGroupPanel = CreateGroupPanel();
        _purchasableStockChartGroupPanel = CreateGroupPanel();

        _allProductsSectionLabel = CreateSectionLabel("Összes termék");
        _purchasableSectionLabel = CreateSectionLabel("Megvásárolható termékek");
        _rentableSectionLabel = CreateSectionLabel("Kölcsönözhető termékek");

        Controls.Add(_titleLabel);
        Controls.Add(_allProductsGroupPanel);
        Controls.Add(_purchasableGroupPanel);
        Controls.Add(_rentableGroupPanel);
        Controls.Add(_ordersChartGroupPanel);
        Controls.Add(_purchasableStockChartGroupPanel);
        Controls.Add(_allProductsSectionLabel);
        Controls.Add(_purchasableSectionLabel);
        Controls.Add(_rentableSectionLabel);

        _totalProductsValue = CreateCard("Összes termék", _allProductsCards);
        ConfigureClickableCard(_totalProductsValue, () => AllProductsRequested?.Invoke());
        _availableProductsValue = CreateCard("Elérhető termékek", _allProductsCards);
        ConfigureClickableCard(_availableProductsValue, () => AvailableProductsRequested?.Invoke());
        _totalStockValue = CreateCard("Összes készlet darabszám", _allProductsCards);
        _ordersChartPanel = CreateChartPanel("Kölcsönözhető termékek foglalásainak aránya", _ordersChartItems);

        _purchasableProductsValue = CreateCard("Megvásárolható termékek", _purchasableCards);
        ConfigureClickableCard(_purchasableProductsValue, () => PurchasableProductsRequested?.Invoke());
        _lowStockValue = CreateCard("Alacsony készlet (<= 2)", _purchasableCards);
        ConfigureClickableCard(_lowStockValue, () => LowStockProductsRequested?.Invoke());
        _outOfStockPurchasableValue = CreateCard("Készlethiányos termékek", _purchasableCards);
        ConfigureClickableCard(_outOfStockPurchasableValue, () => OutOfStockPurchasableProductsRequested?.Invoke());
        _purchasableStockChartPanel = CreateChartPanel("Vásárolható készlet", _purchasableStockChartItems);

        _rentableProductsValue = CreateCard("Kölcsönözhető termékek", _rentableCards);
        ConfigureClickableCard(_rentableProductsValue, () => RentableProductsRequested?.Invoke());
        _paidRentalsValue = CreateCard("Kölcsönzések száma", _rentableCards);
        ConfigureClickableCard(_paidRentalsValue, () => AllOrdersRequested?.Invoke());
        _completedRentalsValue = CreateCard("Befejezett kölcsönzések", _rentableCards);
        ConfigureClickableCard(_completedRentalsValue, () => CompletedOrdersRequested?.Invoke());
        _pendingRentalsValue = CreateCard("Függőben lévő kölcsönzések", _rentableCards);
        ConfigureClickableCard(_pendingRentalsValue, () => PendingOrdersRequested?.Invoke());
        _activeRentalsValue = CreateCard("Aktív kölcsönzések", _rentableCards);
        ConfigureClickableCard(_activeRentalsValue, () => ActiveOrdersRequested?.Invoke());

        Resize += (_, _) => LayoutCards();
        LayoutCards();
    }

    private static Label CreateSectionLabel(string text)
    {
        return new Label
        {
            Text = text,
            Font = new Font("Segoe UI", 13, FontStyle.Bold),
            BackColor = Color.FromArgb(34, 41, 74),
            ForeColor = Color.White,
            AutoSize = false,
            AutoEllipsis = true,
            TextAlign = ContentAlignment.MiddleCenter,
            Padding = new Padding(0)
        };
    }

    private static Panel CreateGroupPanel()
    {
        var panel = new Panel
        {
            BackColor = Color.FromArgb(232, 225, 214),
            BorderStyle = BorderStyle.None
        };
        panel.Paint += (_, e) =>
        {
            ControlPaint.DrawBorder(e.Graphics, panel.ClientRectangle, Color.FromArgb(178, 164, 145), ButtonBorderStyle.Solid);
        };
        return panel;
    }

    private Label CreateCard(string subtitle, List<Panel> targetCollection)
    {
        var card = new Panel
        {
            Size = new Size(250, 138),
            BackColor = Color.White,
            BorderStyle = BorderStyle.None
        };
        card.Paint += (_, e) =>
        {
            ControlPaint.DrawBorder(e.Graphics, card.ClientRectangle, Color.FromArgb(226, 221, 212), ButtonBorderStyle.Solid);
        };

        var value = new Label
        {
            Text = "0",
            Font = new Font("Segoe UI", 24, FontStyle.Bold),
            ForeColor = Color.FromArgb(34, 41, 74),
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Padding = new Padding(8, 0, 8, 0),
            Tag = ValueLabelTag
        };

        var text = new Label
        {
            Text = subtitle,
            Font = new Font("Segoe UI", 11, FontStyle.Regular),
            ForeColor = Color.FromArgb(60, 60, 60),
            AutoSize = false,
            AutoEllipsis = true,
            TextAlign = ContentAlignment.TopCenter,
            Padding = new Padding(14, 4, 14, 4),
            Tag = SubtitleLabelTag
        };

        card.Controls.Add(value);
        card.Controls.Add(text);
        Controls.Add(card);
        targetCollection.Add(card);

        return value;
    }

    private static void ConfigureClickableCard(Label valueLabel, Action clickAction)
    {
        if (valueLabel.Parent is not Panel card)
            return;

        void Attach(Control control)
        {
            control.Cursor = Cursors.Hand;
            control.Click += (_, _) => clickAction();
            control.MouseEnter += (_, _) => card.BackColor = Color.FromArgb(248, 250, 255);
            control.MouseLeave += (_, _) => card.BackColor = Color.White;

            foreach (Control child in control.Controls)
                Attach(child);
        }

        Attach(card);
    }

    private Panel CreateChartPanel(string title, List<DashboardChartItem> items)
    {
        var panel = new Panel
        {
            BackColor = Color.White,
            BorderStyle = BorderStyle.None
        };
        panel.Paint += (_, e) => DrawChartPanel(panel, e.Graphics, title, items);
        Controls.Add(panel);
        return panel;
    }

    private static void DrawChartPanel(Panel panel, Graphics graphics, string title, List<DashboardChartItem> items)
    {
        graphics.Clear(panel.BackColor);
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        ControlPaint.DrawBorder(graphics, panel.ClientRectangle, Color.FromArgb(226, 221, 212), ButtonBorderStyle.Solid);

        using var titleFont = new Font("Segoe UI", 10, FontStyle.Bold);
        using var labelFont = new Font("Segoe UI", 9f, FontStyle.Regular);
        using var emptyFont = new Font("Segoe UI", 10, FontStyle.Italic);
        var textColor = Color.FromArgb(62, 62, 62);
        var headerBounds = new Rectangle(0, 0, panel.ClientSize.Width, UiScale.Dpi(panel, 32));
        using (var headerBrush = new SolidBrush(Color.FromArgb(34, 41, 74)))
        {
            graphics.FillRectangle(headerBrush, headerBounds);
        }
        TextRenderer.DrawText(graphics, title, titleFont, headerBounds, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

        var left = UiScale.Dpi(panel, 18);
        var width = Math.Max(80, panel.ClientSize.Width - (left * 2));

        var total = items.Sum(x => Math.Max(0, x.Value));
        if (total == 0)
        {
            var emptyBounds = new Rectangle(0, headerBounds.Bottom, panel.ClientSize.Width, Math.Max(0, panel.ClientSize.Height - headerBounds.Height));
            TextRenderer.DrawText(graphics, "Nincs megjeleníthető adat", emptyFont, emptyBounds, Color.FromArgb(120, 120, 120), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            return;
        }

        var rowTop = headerBounds.Bottom + UiScale.Dpi(panel, 16);
        var rowHeight = Math.Max(UiScale.Dpi(panel, 30), (panel.ClientSize.Height - rowTop - UiScale.Dpi(panel, 14)) / Math.Max(1, items.Count));
        foreach (var item in items)
        {
            var value = Math.Max(0, item.Value);
            var percentage = value / (double)total;
            var labelText = $"{item.Label}: {value} ({percentage:P0})";
            TextRenderer.DrawText(graphics, labelText, labelFont, new Rectangle(left, rowTop, width, UiScale.Dpi(panel, 20)), textColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            var barTop = rowTop + UiScale.Dpi(panel, 22);
            var barHeight = UiScale.Dpi(panel, 10);
            var barBackground = new Rectangle(left, barTop, width, barHeight);
            using (var backgroundBrush = new SolidBrush(Color.FromArgb(238, 235, 229)))
            using (var barBrush = new SolidBrush(item.Color))
            {
                graphics.FillRectangle(backgroundBrush, barBackground);
                var fillWidth = value == 0 ? 0 : Math.Max(2, (int)Math.Round(width * percentage));
                if (fillWidth > 0)
                    graphics.FillRectangle(barBrush, new Rectangle(left, barTop, fillWidth, barHeight));
            }

            rowTop += rowHeight;
        }
    }

    private void LayoutCards()
    {
        var left = UiScale.Dpi(this, 30);
        var contentWidth = Math.Max(980, ClientSize.Width - (left * 2));
        _titleLabel.Location = new Point(left, UiScale.Dpi(this, 20));
        _titleLabel.Size = new Size(contentWidth, UiScale.Dpi(this, 44));

        var top = UiScale.Dpi(this, 90);
        var columnGap = UiScale.Dpi(this, 42);
        var maxCardCount = new[] { _allProductsCards.Count, _purchasableCards.Count, _rentableCards.Count }.Max();
        var availableColumnHeight = Math.Max(UiScale.Dpi(this, 620), ClientSize.Height - top - UiScale.Dpi(this, 28));
        var cardHeight = Math.Min(UiScale.Dpi(this, 132), Math.Max(UiScale.Dpi(this, 112), (availableColumnHeight - UiScale.Dpi(this, 58)) / Math.Max(1, maxCardCount)));
        var allProductsGroupHeight = GetColumnGroupHeight(this, _allProductsCards.Count, cardHeight);
        var purchasableGroupHeight = GetColumnGroupHeight(this, _purchasableCards.Count, cardHeight);
        var rentableGroupHeight = GetColumnGroupHeight(this, _rentableCards.Count, cardHeight);
        var chartHeight = Math.Max(UiScale.Dpi(this, 170), ((maxCardCount - _allProductsCards.Count) * cardHeight) - UiScale.Dpi(this, 30));
        var columnWidth = (contentWidth - (columnGap * 2)) / 3;

        LayoutGroupPanel(_allProductsGroupPanel, left, top, columnWidth, allProductsGroupHeight);
        LayoutGroupPanel(_purchasableGroupPanel, left + columnWidth + columnGap, top, columnWidth, purchasableGroupHeight);
        LayoutGroupPanel(_rentableGroupPanel, left + ((columnWidth + columnGap) * 2), top, columnWidth, rentableGroupHeight);

        LayoutColumnWithChart(_allProductsSectionLabel, _allProductsCards, _ordersChartGroupPanel, _ordersChartPanel, left, top, columnWidth, cardHeight, chartHeight);
        LayoutColumnWithChart(_purchasableSectionLabel, _purchasableCards, _purchasableStockChartGroupPanel, _purchasableStockChartPanel, left + columnWidth + columnGap, top, columnWidth, cardHeight, chartHeight);
        LayoutColumn(_rentableSectionLabel, _rentableCards, left + ((columnWidth + columnGap) * 2), top, columnWidth, cardHeight);
    }

    private static int GetColumnGroupHeight(Control owner, int cardCount, int cardHeight)
    {
        return UiScale.Dpi(owner, 58) + (cardHeight * cardCount);
    }

    private static void LayoutGroupPanel(Panel groupPanel, int x, int top, int columnWidth, int groupHeight)
    {
        var margin = UiScale.Dpi(groupPanel, 10);
        groupPanel.Location = new Point(x - margin, top - margin);
        groupPanel.Size = new Size(columnWidth + (margin * 2), groupHeight);
        groupPanel.SendToBack();
    }

    private static int LayoutColumn(Label sectionLabel, List<Panel> cards, int x, int top, int columnWidth, int cardHeight)
    {
        sectionLabel.Location = new Point(x, top);
        sectionLabel.Size = new Size(columnWidth, UiScale.Dpi(sectionLabel, 34));

        var cardTop = top + UiScale.Dpi(sectionLabel, 46);
        foreach (var card in cards)
        {
            card.Location = new Point(x, cardTop);
            card.Size = new Size(columnWidth, cardHeight - UiScale.Dpi(card, 12));
            LayoutCardContent(card);
            cardTop += cardHeight;
        }

        return cardTop;
    }

    private static void LayoutColumnWithChart(Label sectionLabel, List<Panel> cards, Panel chartGroupPanel, Panel chartPanel, int x, int top, int columnWidth, int cardHeight, int chartHeight)
    {
        var cardTop = LayoutColumn(sectionLabel, cards, x, top, columnWidth, cardHeight);
        var margin = UiScale.Dpi(chartGroupPanel, 10);
        var chartGroupTop = cardTop + UiScale.Dpi(chartGroupPanel, 12);
        chartGroupPanel.Location = new Point(x - margin, chartGroupTop);
        chartGroupPanel.Size = new Size(columnWidth + (margin * 2), chartHeight + (margin * 2));
        chartGroupPanel.SendToBack();

        chartPanel.Location = new Point(x, chartGroupTop + margin);
        chartPanel.Size = new Size(columnWidth, chartHeight);
    }

    private static void LayoutCardContent(Panel card)
    {
        var value = card.Controls.OfType<Label>().FirstOrDefault(x => Equals(x.Tag, ValueLabelTag));
        var subtitle = card.Controls.OfType<Label>().FirstOrDefault(x => Equals(x.Tag, SubtitleLabelTag));
        if (value is null || subtitle is null)
            return;

        var subtitleHeight = Math.Min(UiScale.Dpi(card, 42), Math.Max(UiScale.Dpi(card, 34), card.Height / 3));
        var valueHeight = Math.Max(UiScale.Dpi(card, 54), card.Height - subtitleHeight);

        value.Bounds = new Rectangle(0, 0, card.Width, valueHeight);
        subtitle.Bounds = new Rectangle(0, value.Bottom, card.Width, Math.Max(1, card.Height - value.Bottom));

        var preferredValueSize = card.Height < UiScale.Dpi(card, 108) ? 23f : 25f;
        value.Font = UiScale.FitFont(
            value,
            value.Text,
            "Segoe UI",
            preferredValueSize,
            17,
            FontStyle.Bold,
            Math.Max(1, value.Width - UiScale.Dpi(value, 16)),
            Math.Max(1, value.Height - UiScale.Dpi(value, 4)));
    }

    public void BindData(List<Product> products, List<Order> orders)
    {
        var paidOrders = orders
            .Where(x => string.Equals(x.Status, "Paid", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(x.StatusName, "Paid", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var purchasableProducts = products.Where(x => !x.IsRentableProduct).ToList();

        _totalProductsValue.Text = products.Count.ToString();
        _availableProductsValue.Text = products.Count(x => x.EffectiveIsAvailable).ToString();
        _totalStockValue.Text = products.Sum(x => Math.Max(0, x.InventoryQuantity)).ToString();

        _purchasableProductsValue.Text = purchasableProducts.Count.ToString();
        _lowStockValue.Text = purchasableProducts.Count(x => x.InventoryQuantity <= 2).ToString();
        _outOfStockPurchasableValue.Text = purchasableProducts.Count(x => x.InventoryQuantity <= 0).ToString();

        _rentableProductsValue.Text = products.Count(x => x.IsRentableProduct).ToString();
        _paidRentalsValue.Text = paidOrders.Count.ToString();
        var completedRentals = paidOrders.Count(x => string.Equals(x.CurrentRentalStatus, "Befejezett", StringComparison.OrdinalIgnoreCase));
        var pendingRentals = paidOrders.Count(x => string.Equals(x.CurrentRentalStatus, "Függőben", StringComparison.OrdinalIgnoreCase));
        var activeRentals = paidOrders.Count(x => string.Equals(x.CurrentRentalStatus, "Aktív", StringComparison.OrdinalIgnoreCase));
        _completedRentalsValue.Text = completedRentals.ToString();
        _pendingRentalsValue.Text = pendingRentals.ToString();
        _activeRentalsValue.Text = activeRentals.ToString();

        var paidOrderCount = orders.Count(x => string.Equals(x.Status, "Paid", StringComparison.OrdinalIgnoreCase) ||
                                               string.Equals(x.StatusName, "Paid", StringComparison.OrdinalIgnoreCase));
        var draftOrderCount = orders.Count(x => string.Equals(x.Status, "Draft", StringComparison.OrdinalIgnoreCase) ||
                                                string.Equals(x.StatusName, "Draft", StringComparison.OrdinalIgnoreCase));
        SetChartItems(
            _ordersChartItems,
            new DashboardChartItem("Fizetett", paidOrderCount, Color.FromArgb(72, 120, 80)),
            new DashboardChartItem("Kosárban", draftOrderCount, Color.FromArgb(202, 162, 107)));

        SetChartItems(
            _purchasableStockChartItems,
            new DashboardChartItem("Rendben", purchasableProducts.Count(x => x.InventoryQuantity > 2), Color.FromArgb(72, 120, 80)),
            new DashboardChartItem("Alacsony", purchasableProducts.Count(x => x.InventoryQuantity > 0 && x.InventoryQuantity <= 2), Color.FromArgb(202, 162, 107)),
            new DashboardChartItem("Nincs készleten", purchasableProducts.Count(x => x.InventoryQuantity <= 0), Color.FromArgb(176, 42, 55)));

        _ordersChartPanel.Invalidate();
        _purchasableStockChartPanel.Invalidate();
    }

    private static void SetChartItems(List<DashboardChartItem> target, params DashboardChartItem[] items)
    {
        target.Clear();
        target.AddRange(items);
    }

    private sealed class DashboardChartItem
    {
        public DashboardChartItem(string label, int value, Color color)
        {
            Label = label;
            Value = value;
            Color = color;
        }

        public string Label { get; }
        public int Value { get; }
        public Color Color { get; }
    }
}
