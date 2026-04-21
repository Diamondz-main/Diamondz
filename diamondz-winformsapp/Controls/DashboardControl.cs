using DiamondzWinForms.Models;

namespace DiamondzWinForms.Controls;

public class DashboardControl : UserControl
{
    private readonly Label _titleLabel;
    private readonly Label _totalProductsValue;
    private readonly Label _activeOrdersValue;
    private readonly Label _availableProductsValue;
    private readonly Label _missingInventoryValue;
    private readonly Label _totalStockValue;
    private readonly Label _lowStockValue;
    private readonly List<Panel> _cards = new();

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
            TextAlign = ContentAlignment.MiddleCenter
        };

        Controls.Add(_titleLabel);

        _totalProductsValue = CreateCard("Összes termék");
        _activeOrdersValue = CreateCard("Rendelések száma");
        _availableProductsValue = CreateCard("Elérhető termékek");
        _missingInventoryValue = CreateCard("Hiányzó inventory rekordok");
        _totalStockValue = CreateCard("Összes készlet darabszám");
        _lowStockValue = CreateCard("Alacsony készlet (<= 2)");

        Resize += (_, _) => LayoutCards();
        LayoutCards();
    }

    private Label CreateCard(string subtitle)
    {
        var card = new Panel
        {
            Size = new Size(250, 150),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        var value = new Label
        {
            Text = "0",
            Font = new Font("Segoe UI", 28, FontStyle.Bold),
            ForeColor = Color.FromArgb(34, 41, 74),
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(20, 28),
            Size = new Size(208, 52)
        };

        var text = new Label
        {
            Text = subtitle,
            Font = new Font("Segoe UI", 11, FontStyle.Regular),
            ForeColor = Color.FromArgb(60, 60, 60),
            AutoSize = false,
            TextAlign = ContentAlignment.TopCenter,
            Location = new Point(16, 92),
            Size = new Size(216, 40)
        };

        card.Controls.Add(value);
        card.Controls.Add(text);
        Controls.Add(card);
        _cards.Add(card);

        return value;
    }

    private void LayoutCards()
    {
        var left = 30;
        var width = Math.Max(900, ClientSize.Width - (left * 2));
        _titleLabel.Location = new Point(left, 20);
        _titleLabel.Size = new Size(width, 44);

        var top = 90;
        var gap = 28;
        var cardWidth = 250;
        var rowHeight = 178;
        var cardsPerRow = Math.Max(1, Math.Min(3, _cards.Count));
        var totalRowWidth = (cardsPerRow * cardWidth) + ((cardsPerRow - 1) * gap);
        var startX = Math.Max(left, (ClientSize.Width - totalRowWidth) / 2);

        for (var i = 0; i < _cards.Count; i++)
        {
            var row = i / cardsPerRow;
            var column = i % cardsPerRow;
            _cards[i].Location = new Point(startX + column * (cardWidth + gap), top + row * rowHeight);
        }
    }

    public void BindData(List<Product> products, List<Order> orders)
    {
        _totalProductsValue.Text = products.Count.ToString();
        _activeOrdersValue.Text = orders.Count.ToString();
        _availableProductsValue.Text = products.Count(x => x.EffectiveIsAvailable).ToString();
        _missingInventoryValue.Text = products.Count(x => string.IsNullOrWhiteSpace(x.InventoryBvin)).ToString();
        _totalStockValue.Text = products.Sum(x => Math.Max(0, x.InventoryQuantity)).ToString();
        _lowStockValue.Text = products.Count(x => x.InventoryQuantity <= 2).ToString();
    }
}