using DiamondzWinForms.Models;

namespace DiamondzWinForms.Controls;

public class OrdersControl : UserControl
{
    private readonly Label _titleLabel;
    private readonly DataGridView _grid;

    public OrdersControl()
    {
        Dock = DockStyle.Fill;
        BackColor = Color.FromArgb(246, 243, 239);

        _titleLabel = new Label
        {
            Text = "K\u00f6lcs\u00f6nz\u00e9sek / Rendel\u00e9sek",
            Font = new Font("Segoe UI", 20, FontStyle.Bold),
            ForeColor = Color.FromArgb(34, 41, 74),
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter
        };

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
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };

        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Rendel\u00e9ssz\u00e1m", DataPropertyName = "OrderNumber", FillWeight = 80 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "V\u00e1s\u00e1rl\u00f3", DataPropertyName = "CustomerName", FillWeight = 120 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Email", DataPropertyName = "UserEmail", FillWeight = 150 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "D\u00e1tum", DataPropertyName = "TimeOfOrderUtc", FillWeight = 90, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd HH:mm" } });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "St\u00e1tusz", DataPropertyName = "StatusName", FillWeight = 80 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "V\u00e9g\u00f6sszeg", DataPropertyName = "TotalGrand", FillWeight = 90, DefaultCellStyle = new DataGridViewCellStyle { Format = "N0" } });

        Controls.Add(_titleLabel);
        Controls.Add(_grid);

        Resize += (_, _) => LayoutResponsive();
        LayoutResponsive();
    }

    private void LayoutResponsive()
    {
        var left = 30;
        var width = Math.Max(900, ClientSize.Width - (left * 2));
        _titleLabel.Location = new Point(left, 20);
        _titleLabel.Size = new Size(width, 44);
        _grid.Location = new Point(left, 80);
        _grid.Size = new Size(width, Math.Max(420, ClientSize.Height - 110));
    }

    public void BindData(List<Order> orders)
    {
        _grid.DataSource = null;
        _grid.DataSource = orders;
    }
}