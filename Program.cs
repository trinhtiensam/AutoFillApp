using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;
using System.Windows.Automation; // UI Automation

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}

public class MainForm : Form
{
    private ComboBox cboWindows = new ComboBox();
    private Button btnRefresh = new Button();
    private Button btnScan = new Button();
    private Button btnFill = new Button();
    private Button btnSaveProfile = new Button();
    private Button btnLoadProfile = new Button();
    private ListView lvFields = new ListView();
    private DataGridView gridData = new DataGridView();

    private List<UiField> detectedFields = new List<UiField>();

    public MainForm()
    {
        Text = "AutoFill App (UI Automation)";
        Width = 1100;
        Height = 700;
        StartPosition = FormStartPosition.CenterScreen;

        // Top panel: window picker + actions
        var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 50, Padding = new Padding(10) };
        cboWindows.Width = 500;
        btnRefresh.Text = "Làm mới danh sách";
        btnScan.Text = "Quét ô nhập";
        btnFill.Text = "Điền dữ liệu";
        btnSaveProfile.Text = "Lưu profile";
        btnLoadProfile.Text = "Tải profile";

        btnRefresh.Click += (_, __) => LoadWindows();
        btnScan.Click += (_, __) => ScanFields();
        btnFill.Click += (_, __) => FillFields();
        btnSaveProfile.Click += (_, __) => SaveProfile();
        btnLoadProfile.Click += (_, __) => LoadProfile();

        top.Controls.Add(new Label { Text = "Chọn cửa sổ:", AutoSize = true, Padding = new Padding(0, 8, 6, 0) });
        top.Controls.Add(cboWindows);
        top.Controls.Add(btnRefresh);
        top.Controls.Add(btnScan);
        top.Controls.Add(btnFill);
        top.Controls.Add(btnSaveProfile);
        top.Controls.Add(btnLoadProfile);

        // Split: left fields, right data grid
        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical };

        // Left: detected UI fields
        lvFields.View = View.Details;
        lvFields.FullRowSelect = true;
        lvFields.GridLines = true;
        lvFields.Columns.Add("Key gợi ý", 160);
        lvFields.Columns.Add("Name", 200);
        lvFields.Columns.Add("AutomationId", 160);
        lvFields.Columns.Add("ControlType", 120);
        lvFields.Columns.Add("Path", 300);
        split.Panel1.Controls.Add(lvFields);
        lvFields.Dock = DockStyle.Fill;

        // Right: key-value
        gridData.AllowUserToAddRows = true;
        gridData.AllowUserToDeleteRows = true;
        gridData.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        gridData.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Key", Name = "Key" });
        gridData.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Value", Name = "Value" });
        split.Panel2.Controls.Add(gridData);
        gridData.Dock = DockStyle.Fill;

        Controls.Add(split);
        Controls.Add(top);

        Load += (_, __) => LoadWindows();
    }

    private void LoadWindows()
    {
        cboWindows.Items.Clear();
        var root = AutomationElement.RootElement;
        if (root == null) return;
        var tops = root.FindAll(TreeScope.Children, Condition.TrueCondition);
        for (int i = 0; i < tops.Count; i++)
        {
            var el = tops[i];
            try
            {
                var name = el.Current.Name;
                if (string.IsNullOrWhiteSpace(name)) continue;
                // Loại bỏ cửa sổ ẩn/không tương tác
                if (!el.Current.IsEnabled) continue;
                cboWindows.Items.Add(new WindowItem(name, el));
            }
            catch { }
        }
        if (cboWindows.Items.Count > 0) cboWindows.SelectedIndex = 0;
    }

    private void ScanFields()
    {
        lvFields.Items.Clear();
        detectedFields.Clear();
        var win = cboWindows.SelectedItem as WindowItem;
        if (win == null)
        {
            MessageBox.Show("Chưa chọn cửa sổ.");
            return;
        }

        var conditions = new OrCondition(
            new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit),
            new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.ComboBox)
        );

        var descendants = win.Element.FindAll(TreeScope.Descendants, conditions);
        for (int i = 0; i < descendants.Count; i++)
        {
            var el = descendants[i];
            try
            {
                var info = UiField.FromElement(el);
                detectedFields.Add(info);
                var item = new ListViewItem(new[]
                {
                    info.SuggestedKey,
                    info.Name ?? string.Empty,
                    info.AutomationId ?? string.Empty,
                    info.ControlType,
                    info.Path
                });
                lvFields.Items.Add(item);
            }
            catch { }
        }

        MessageBox.Show($"Đã phát hiện {detectedFields.Count} ô nhập.");
    }

    private Dictionary<string, string> ReadGridData()
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (DataGridViewRow row in gridData.Rows)
        {
            if (row.IsNewRow) continue;
            var key = row.Cells["Key"].Value?.ToString()?.Trim();
            var val = row.Cells["Value"].Value?.ToString() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(key))
            {
                dict[key] = val;
            }
        }
        return dict;
    }

    private void FillFields()
    {
        var data = ReadGridData();
        if (data.Count == 0)
        {
            MessageBox.Show("Chưa có dữ liệu (key/value) để điền.");
            return;
        }
        if (detectedFields.Count == 0)
        {
            MessageBox.Show("Chưa quét ô nhập. Nhấn 'Quét ô nhập' trước.");
            return;
        }

        int filled = 0;
        foreach (var f in detectedFields)
        {
            // Tìm value theo key phù hợp
            var key = f.SuggestedKey;
            string? matchedKey = null;

            // 1) khớp chính xác key
            if (data.ContainsKey(key)) matchedKey = key;
            else
            {
                // 2) thử khớp theo chứa chuỗi trong AutomationId/Name
                foreach (var k in data.Keys)
                {
                    if (f.MatchKey(k)) { matchedKey = k; break; }
                }
            }
            if (matchedKey == null) continue;
            var value = data[matchedKey];

            try
            {
                // Ưu tiên ValuePattern
                if (f.Element.TryGetCurrentPattern(ValuePattern.Pattern, out var patObj) && patObj is ValuePattern vp)
                {
                    if (!f.Element.Current.IsEnabled || f.Element.Current.IsOffscreen) continue;
                    vp.SetValue(value);
                    filled++;
                }
                else
                {
                    // Fallback: focus + gửi phím (không phải app nào cũng cho phép)
                    f.Element.SetFocus();
                    SendKeys.SendWait("^a");
                    SendKeys.SendWait("{DEL}");
                    SendKeys.SendWait(value);
                    filled++;
                }
            }
            catch { /* bỏ qua ô lỗi */ }
        }

        MessageBox.Show($"Đã điền {filled} ô.");
    }

    private void SaveProfile()
    {
        var sfd = new SaveFileDialog
        {
            Filter = "JSON (*.json)|*.json",
            FileName = "profile.json"
        };
        if (sfd.ShowDialog() == DialogResult.OK)
        {
            var payload = new Profile
            {
                Data = ReadGridData()
            };
            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(sfd.FileName, json);
            MessageBox.Show("Đã lưu profile.");
        }
    }

    private void LoadProfile()
    {
        var ofd = new OpenFileDialog
        {
            Filter = "JSON (*.json)|*.json"
        };
        if (ofd.ShowDialog() == DialogResult.OK)
        {
            var json = File.ReadAllText(ofd.FileName);
            var payload = JsonSerializer.Deserialize<Profile>(json) ?? new Profile();
            gridData.Rows.Clear();
            foreach (var kv in payload.Data)
            {
                gridData.Rows.Add(kv.Key, kv.Value);
            }
            MessageBox.Show("Đã tải profile.");
        }
    }
}

public class WindowItem
{
    public string Title { get; }
    public AutomationElement Element { get; }
    public WindowItem(string title, AutomationElement element)
    {
        Title = title; Element = element;
    }
    public override string ToString() => Title;
}

public class UiField
{
    public AutomationElement Element { get; set; }
    public string? Name { get; set; }
    public string? AutomationId { get; set; }
    public string ControlType { get; set; } = "";
    public string Path { get; set; } = ""; // đường dẫn tương đối trong cây UIA (để debug)

    public string SuggestedKey
    {
        get
        {
            // heuristic đơn giản: ưu tiên AutomationId; fallback Name; chuyển về dạng key gọn
            var src = !string.IsNullOrWhiteSpace(AutomationId) ? AutomationId : (Name ?? "");
            src = src.ToLowerInvariant();
            // lọc ký tự lạ, thay khoảng trắng bằng _
            var key = new string(src.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());
            key = key.Trim('_');
            if (string.IsNullOrEmpty(key)) key = ControlType.ToLowerInvariant();
            return key;
        }
    }

    public static UiField FromElement(AutomationElement el)
    {
        var info = new UiField { Element = el };
        try { info.Name = el.Current.Name; } catch { }
        try { info.AutomationId = el.Current.AutomationId; } catch { }
        try { info.ControlType = el.Current.ControlType?.ProgrammaticName ?? ""; } catch { }
        info.Path = BuildPath(el);
        return info;
    }

    public bool MatchKey(string key)
    {
        key = key.ToLowerInvariant();
        bool In(string? s) => !string.IsNullOrWhiteSpace(s) && s!.ToLowerInvariant().Contains(key);
        return In(AutomationId) || In(Name) || In(ControlType);
    }

    private static string BuildPath(AutomationElement el)
    {
        // Dùng để debug: lấy vài cấp cha với Name/AutomationId
        var parts = new List<string>();
        var cur = el;
        for (int i = 0; i < 4; i++)
        {
            try
            {
                var n = cur.Current.Name;
                var a = cur.Current.AutomationId;
                var t = cur.Current.ControlType?.ProgrammaticName ?? "";
                parts.Add($"[{t}]#{a}:{n}");
            }
            catch { break; }
            var parent = TreeWalker.ControlViewWalker.GetParent(cur);
            if (parent == null) break;
            cur = parent;
        }
        parts.Reverse();
        return string.Join(" > ", parts);
    }
}

public class Profile
{
    public Dictionary<string, string> Data { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
