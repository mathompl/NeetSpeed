using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Net.NetworkInformation;
using System.Windows.Forms;

namespace InterfaceTrafficWatch
{
    public partial class MainForm : Form
    {
        public static MainForm Instance { get; private set; }
        private int timerUpdate = 500;
        private NetworkInterface nic;
        private int maxDl = 1000;
        private int maxUp = 200;
        private bool displayInBits = false; // false = bytes (KB/s), true = bits (Kb/s)

        private System.Timers.Timer timer;
        private readonly List<int> dataIn = new List<int>();
        private readonly List<int> dataOut = new List<int>();

        private Bitmap canvas;
        private Image frameBackground;

        private readonly SolidBrush brushBg = new SolidBrush(Color.FromArgb(30, 30, 30));
        private readonly SolidBrush brushUp = new SolidBrush(Color.FromArgb(220, 50, 50));
        private readonly SolidBrush brushDl = new SolidBrush(Color.FromArgb(50, 200, 80));
        private readonly SolidBrush brushText = new SolidBrush(Color.Yellow);
        private readonly Font font = new Font("Consolas", 7f, FontStyle.Bold);

        private long bytesSentLast = 0;
        private long bytesReceivedLast = 0;

        private Point dragOffset;
        private bool isDragging = false;
        private bool hidden = false;
        private bool topMostState = false;

        public MainForm()
        {
            try
            {
                InitializeComponent();
                this.DoubleBuffered = true;
                this.FormBorderStyle = FormBorderStyle.None;
                this.TransparencyKey = Color.FromArgb(255, 128, 128);
                this.BackColor = Color.FromArgb(255, 128, 128);
                this.BackgroundImageLayout = ImageLayout.None;

                try
                {
                    frameBackground = this.BackgroundImage;
                    // Localize menu and window title from resources (use ResourceManager)
                    try
                    {
                        var rm = NetSpeed.Properties.Resources.ResourceManager;
                        var rc = NetSpeed.Properties.Resources.Culture;
                        this.Text = rm.GetString("MainForm_Title", rc) ?? this.Text;
                        Minimalizuj.Text = rm.GetString("Menu_Minimize", rc) ?? Minimalizuj.Text;
                        toolStripMenuItem1.Text = rm.GetString("Menu_AlwaysOnTop", rc) ?? toolStripMenuItem1.Text;
                        toolStripMenuItem2.Text = rm.GetString("Menu_Config", rc) ?? toolStripMenuItem2.Text;
                        toolStripMenuItem3.Text = rm.GetString("Menu_About", rc) ?? toolStripMenuItem3.Text;
                        toolStripMenuItem4.Text = rm.GetString("Menu_Exit", rc) ?? toolStripMenuItem4.Text;
                    }
                    catch { }

                    frameBackground = this.BackgroundImage;
                    if (frameBackground == null)
                        frameBackground = NetSpeed.Properties.Resources.metal1;
                }
                catch
                {
                    frameBackground = this.BackgroundImage;
                }

                // register instance for runtime culture updates
                Instance = this;

                ReloadConfig();
                InitializeNetwork();
                InitializeTimer();

                if (this.Width > 0 && this.Height > 0)
                {
                    canvas = new Bitmap(this.Width, this.Height);
                    this.BackgroundImage = canvas;
                }
            }
            catch (Exception ex)
            {
                LogError("Konstruktor", ex);
                MessageBox.Show("Błąd inicjalizacji aplikacji:\n" + ex.Message, "Błąd",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void ApplyCulture(string culture = null)
        {
            try
            {
                if (!string.IsNullOrEmpty(culture))
                {
                    try
                    {
                        var ci = new System.Globalization.CultureInfo(culture);
                        NetSpeed.Properties.Resources.Culture = ci;
                        System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = ci;
                        System.Threading.Thread.CurrentThread.CurrentUICulture = ci;
                    }
                    catch { }
                }

                var rm = NetSpeed.Properties.Resources.ResourceManager;
                var rc = NetSpeed.Properties.Resources.Culture;

                // Update menu texts and title
                try
                {
                    this.Text = rm.GetString("MainForm_Title", rc) ?? this.Text;
                    Minimalizuj.Text = rm.GetString("Menu_Minimize", rc) ?? Minimalizuj.Text;
                    toolStripMenuItem1.Text = rm.GetString("Menu_AlwaysOnTop", rc) ?? toolStripMenuItem1.Text;
                    toolStripMenuItem2.Text = rm.GetString("Menu_Config", rc) ?? toolStripMenuItem2.Text;
                    toolStripMenuItem3.Text = rm.GetString("Menu_About", rc) ?? toolStripMenuItem3.Text;
                    toolStripMenuItem4.Text = rm.GetString("Menu_Exit", rc) ?? toolStripMenuItem4.Text;
                }
                catch { }

                // Force UI refresh
                this.Invalidate();
                this.Refresh();

                // Notify other open forms (call OnCultureChanged if present)
                foreach (System.Windows.Forms.Form f in System.Windows.Forms.Application.OpenForms)
                {
                    if (f == this) continue;
                    try
                    {
                        var mi = f.GetType().GetMethod("OnCultureChanged", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                        mi?.Invoke(f, new object[] { });
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                LogError("ApplyCulture", ex);
            }
        }

        private void LogError(string context, Exception ex)
        {
            Debug.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + context + ": " + ex.Message);
        }

        public void ReloadConfig()
        {
            try
            {
                var reg = Application.UserAppDataRegistry;

                if (reg.GetValue("X") is int x && reg.GetValue("Y") is int y)
                {
                    this.Left = x;
                    this.Top = y;
                }

                if (reg.GetValue("MaxDL") is int mdl) maxDl = Math.Max(1, mdl);
                if (reg.GetValue("MaxUP") is int mup) maxUp = Math.Max(1, mup);
                if (reg.GetValue("DisplayInBits") is int dib) displayInBits = dib != 0;


                if (reg.GetValue("Timer") is int t)
                {
                    timerUpdate = Math.Max(200, t);
                    if (timer != null)
                    {
                        timer.Stop();
                        timer.Dispose();
                        InitializeTimer();
                    }
                }

                if (reg.GetValue("Interface") is string nicName && !string.IsNullOrWhiteSpace(nicName))
                {
                    foreach (var n in NetworkInterface.GetAllNetworkInterfaces())
                    {
                        if (n.Name == nicName)
                        {
                            nic = n;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogError("ReloadConfig", ex);
            }
        }

        private void InitializeNetwork()
        {
            try
            {
                if (nic != null && nic.OperationalStatus == OperationalStatus.Up)
                    return;

                NetworkInterface best = null;
                long bestTraffic = -1;

                foreach (var n in NetworkInterface.GetAllNetworkInterfaces())
                {
                    try
                    {
                        if (n.OperationalStatus != OperationalStatus.Up) continue;
                        if (n.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;

                        var stats = n.GetIPv4Statistics();
                        long traffic = stats.BytesReceived + stats.BytesSent;

                        if (traffic > bestTraffic)
                        {
                            bestTraffic = traffic;
                            best = n;
                        }
                    }
                    catch (NetworkInformationException) { }
                    catch (Exception ex)
                    {
                        LogError("Sprawdzanie interfejsu " + n.Name, ex);
                    }
                }

                nic = best;

                if (nic == null)
                {
                    var all = NetworkInterface.GetAllNetworkInterfaces();
                    if (all.Length > 0)
                        nic = all[0];
                }
            }
            catch (Exception ex)
            {
                LogError("InitializeNetwork", ex);
                nic = null;
            }
        }

        private void InitializeTimer()
        {
            try
            {
                // Use System.Timers.Timer to decouple sampling from the UI message loop
                if (timer != null)
                {
                    try { timer.Stop(); timer.Dispose(); } catch { }
                }

                timer = new System.Timers.Timer(timerUpdate);
                timer.AutoReset = true;
                timer.Elapsed += (s, e) =>
                {
                    try
                    {
                        // Marshal to UI thread for safe UI updates
                        if (!this.IsDisposed && this.IsHandleCreated)
                        {
                            try { this.BeginInvoke((Action)(() => { try { UpdateNetworkInterface(); } catch (Exception ex) { LogError("Timer Tick", ex); } })); }
                            catch { }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogError("Timer Elapsed", ex);
                    }
                };
                timer.Start();
            }
            catch (Exception ex)
            {
                LogError("InitializeTimer", ex);
            }
        }

        private void paintChart(Graphics g, Brush color, int x, int y, int width, int height, List<int> data)
        {
            try
            {
                if (data == null || data.Count < 2 || width <= 0 || height <= 0) return;

                while (data.Count > width)
                    data.RemoveAt(0);

                int maxValue = 1;
                foreach (var v in data)
                    if (v > maxValue) maxValue = v;

                float scale = (float)height / maxValue;

                using (var pen = new Pen(color, 1.5f))
                {
                    for (int i = 1; i < data.Count; i++)
                    {
                        float x1 = x + width - (data.Count - i + 1);
                        float y1 = y + height - data[i - 1] * scale;
                        float x2 = x + width - (data.Count - i);
                        float y2 = y + height - data[i] * scale;
                        g.DrawLine(pen, x1, y1, x2, y2);
                    }
                }
            }
            catch (Exception ex)
            {
                LogError("paintChart", ex);
            }
        }

        private void paintCharts(string sent, string received)
        {
            try
            {
                if (this.Width <= 0 || this.Height <= 0) return;

                if (canvas == null || canvas.Width != this.Width || canvas.Height != this.Height)
                {
                    canvas?.Dispose();
                    canvas = new Bitmap(this.Width, this.Height);
                    this.BackgroundImage = canvas;
                }

                using (Graphics g = Graphics.FromImage(canvas))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                    if (frameBackground != null)
                        g.DrawImage(frameBackground, 0, 0, this.Width, this.Height);
                    else
                        g.Clear(Color.FromArgb(255, 128, 128));

                    int pad = 8;
                    int innerW = this.Width - pad * 2;
                    int innerH = this.Height - pad * 2;

                    g.FillRectangle(brushBg, pad, pad, innerW, innerH);

                    using (var gridPen = new Pen(Color.FromArgb(40, 40, 60), 1))
                    {
                        for (int i = pad + 4; i < this.Width - pad; i += 6)
                            g.DrawLine(gridPen, i, pad + 2, i, this.Height - pad - 2);
                        for (int k = pad + 4; k < this.Height - pad; k += 6)
                            g.DrawLine(gridPen, pad + 2, k, this.Width - pad - 2, k);
                    }

                    g.DrawString(received + " in", font, brushText, pad + 4, pad + 2);
                    // localize "in" / "out"
                    var rm = NetSpeed.Properties.Resources.ResourceManager;
                    var rc = NetSpeed.Properties.Resources.Culture;
                    string inLabel = rm.GetString("MainForm_In", rc) ?? "in";
                    string outLabel = rm.GetString("MainForm_Out", rc) ?? "out";
                    g.DrawString(received + " " + inLabel, font, brushText, pad + 4, pad + 2);
                    g.DrawString(sent + " " + outLabel, font, brushText, pad + 4, this.Height / 2 + 2);

                    using (var midPen = new Pen(Color.Gray, 1))
                        g.DrawLine(midPen, pad, this.Height / 2, this.Width - pad, this.Height / 2);

                    paintChart(g, brushDl, pad + 2, pad + 16, innerW - 4, this.Height / 2 - pad - 20, dataIn);
                    paintChart(g, brushUp, pad + 2, this.Height / 2 + pad + 8, innerW - 4, this.Height / 2 - pad - 20, dataOut);
                }

                this.Invalidate();
            }
            catch (Exception ex)
            {
                LogError("paintCharts", ex);
            }
        }

        private void paintTray(double dlSpeed, double upSpeed)
        {
            try
            {
                dlSpeed = Math.Max(0, dlSpeed);
                upSpeed = Math.Max(0, upSpeed);

                using (Bitmap bmp = new Bitmap(16, 16))
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.FromArgb(40, 40, 40));
                    g.DrawRectangle(Pens.Black, 0, 0, 15, 15);

                    int scaleDl = Math.Min(14, Math.Max(0, (int)(dlSpeed / Math.Max(1, maxDl) * 14)));
                    int scaleUp = Math.Min(14, Math.Max(0, (int)(upSpeed / Math.Max(1, maxUp) * 14)));

                    if (scaleDl > 0)
                        g.FillRectangle(brushDl, 2, 15 - scaleDl, 5, scaleDl);
                    if (scaleUp > 0)
                        g.FillRectangle(brushUp, 9, 15 - scaleUp, 5, scaleUp);

                    notifyIcon1.Icon = Icon.FromHandle(bmp.GetHicon());
                }

                // Format speeds for display (convert from KiB/s to Kb/s/Mb/s as needed)
                string dlText = FormatSpeed(dlSpeed);
                string upText = FormatSpeed(upSpeed);
                string trayFmt = NetSpeed.Properties.Resources.ResourceManager.GetString("MainForm_TrayFormat", NetSpeed.Properties.Resources.Culture) ?? "DL {0} UP {1}";
                string text = string.Format(trayFmt, dlText, upText);
                if (text.Length > 63)
                    text = text.Substring(0, 63);

                notifyIcon1.Text = text;
            }
            catch (Exception ex)
            {
                LogError("paintTray", ex);
            }
        }

        private string FormatSpeed(double kibPerSec)
        {
            try
            {
                if (double.IsNaN(kibPerSec) || double.IsInfinity(kibPerSec))
                    return "---";

                if (displayInBits)
                {
                    // Convert KiB/s -> kilobits/s
                    double kilobits = kibPerSec * 8.0;
                    if (kilobits >= 1000.0)
                    {
                        double mb = kilobits / 1000.0;
                        return string.Format("{0:F1} Mb/s", mb);
                    }
                    else
                    {
                        return string.Format("{0:F0} Kb/s", kilobits);
                    }
                }
                else
                {
                    // Show in bytes: KB/s or MB/s (using 1024)
                    if (kibPerSec >= 1024.0)
                    {
                        double mb = kibPerSec / 1024.0;
                        return string.Format("{0:F1} MB/s", mb);
                    }
                    else
                    {
                        return string.Format("{0:F0} KB/s", kibPerSec);
                    }
                }
            }
            catch
            {
                return "---";
            }
        }

        private void UpdateNetworkInterface()
        {
            try
            {
                if (nic == null || nic.OperationalStatus != OperationalStatus.Up)
                {
                    InitializeNetwork();
                    if (nic == null)
                    {
                        paintCharts("---", "---");
                        return;
                    }
                }

                IPv4InterfaceStatistics stats;
                try
                {
                    stats = nic.GetIPv4Statistics();
                }
                catch (NetworkInformationException)
                {
                    nic = null;
                    bytesSentLast = 0;
                    bytesReceivedLast = 0;
                    InitializeNetwork();
                    return;
                }

                long bytesSent = stats.BytesSent;
                long bytesReceived = stats.BytesReceived;

                bool reset =
                    bytesSentLast == 0 ||
                    bytesReceivedLast == 0 ||
                    bytesSent < bytesSentLast ||
                    bytesReceived < bytesReceivedLast ||
                    (bytesSent - bytesSentLast) > 100L * 1024 * 1024 ||
                    (bytesReceived - bytesReceivedLast) > 500L * 1024 * 1024;

                if (reset)
                {
                    bytesSentLast = bytesSent;
                    bytesReceivedLast = bytesReceived;
                    return;
                }

                double factor = 1000.0 / Math.Max(1, timerUpdate);
                double bytesSentSpeed = Math.Max(0, (bytesSent - bytesSentLast) / 1024.0 * factor);
                double bytesReceivedSpeed = Math.Max(0, (bytesReceived - bytesReceivedLast) / 1024.0 * factor);

                if (bytesSentSpeed > 200000) bytesSentSpeed = 0;
                if (bytesReceivedSpeed > 200000) bytesReceivedSpeed = 0;

                dataIn.Add((int)bytesReceivedSpeed);
                dataOut.Add((int)bytesSentSpeed);

                // Prepare human-friendly strings for chart/tray (switch to Mb/s when large)
                string sent = FormatSpeed(bytesSentSpeed);
                string received = FormatSpeed(bytesReceivedSpeed);

                paintTray(bytesReceivedSpeed, bytesSentSpeed);
                paintCharts(sent, received);

                bytesSentLast = bytesSent;
                bytesReceivedLast = bytesReceived;
            }
            catch (Exception ex)
            {
                LogError("UpdateNetworkInterface", ex);
                bytesSentLast = 0;
                bytesReceivedLast = 0;
            }
        }

        private void MainForm_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDragging = true;
                dragOffset = e.Location;
            }
        }

        private void MainForm_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging && e.Button == MouseButtons.Left)
            {
                this.Left += e.X - dragOffset.X;
                this.Top += e.Y - dragOffset.Y;
            }
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
                Hide();
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Minimalizuj_Click(sender, e);
        }

        private void toolStripMenuItem4_Click(object sender, EventArgs e)
        {
            try
            {
                writeConfig();
                Application.Exit();
            }
            catch (Exception ex)
            {
                LogError("Wyjście", ex);
                Application.Exit();
            }
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            try
            {
                writeConfig();
                using (var setup = new Setup())
                {
                    setup.ShowDialog();
                }
                ReloadConfig();
                InitializeNetwork();
                bytesSentLast = 0;
                bytesReceivedLast = 0;
            }
            catch (Exception ex)
            {
                LogError("Setup", ex);
                MessageBox.Show("Błąd otwierania ustawień:\n" + ex.Message);
            }
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            try
            {
                topMostState = !topMostState;
                this.TopMost = topMostState;
                toolStripMenuItem1.Checked = topMostState;
                toolStripMenuItem1.Font = new Font(toolStripMenuItem1.Font,
                    topMostState ? FontStyle.Bold : FontStyle.Regular);
            }
            catch (Exception ex)
            {
                LogError("TopMost", ex);
            }
        }

        private void Minimalizuj_Click(object sender, EventArgs e)
        {
            try
            {
                if (hidden)
                {
                    Show();
                    hidden = false;
                    Minimalizuj.Text = "Minimalizuj";
                }
                else
                {
                    Hide();
                    hidden = true;
                    Minimalizuj.Text = "Pokaż";
                }
            }
            catch (Exception ex)
            {
                LogError("Minimalizuj", ex);
            }
        }

        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            try
            {
                using (var about = new AboutBox1())
                {
                    about.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                LogError("About", ex);
            }
        }

        private void writeConfig()
        {
            try
            {
                var reg = Application.UserAppDataRegistry;
                reg.SetValue("X", this.Left);
                reg.SetValue("Y", this.Top);
                reg.SetValue("Minimized", hidden);
                reg.SetValue("MaxDL", maxDl);
                reg.SetValue("MaxUP", maxUp);
                reg.SetValue("Timer", timerUpdate);

                if (nic != null)
                    reg.SetValue("Interface", nic.Name);
            }
            catch (Exception ex)
            {
                LogError("writeConfig", ex);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                timer?.Stop();
                timer?.Dispose();
                canvas?.Dispose();
                brushBg?.Dispose();
                brushUp?.Dispose();
                brushDl?.Dispose();
                brushText?.Dispose();
                font?.Dispose();
                writeConfig();
            }
            catch (Exception ex)
            {
                LogError("OnFormClosing", ex);
            }
            base.OnFormClosing(e);

        }

        // ===== puste metody pod stary Designer =====
        private void MainForm_Load(object sender, EventArgs e) { }
        private void MainForm_Paint(object sender, PaintEventArgs e) { }
        private void MainForm_Leave(object sender, EventArgs e) { }
        private void MainForm_MouseClick(object sender, MouseEventArgs e) { }
        private void notifyIcon1_MouseClick(object sender, MouseEventArgs e) { }

        private void toolStripMenuItem1_MouseDown(object sender, MouseEventArgs e)
        {
            MainForm_MouseDown(this, e);
        }

        private void toolStripMenuItem1_MouseMove(object sender, MouseEventArgs e)
        {
            MainForm_MouseMove(this, e);
        }
    }
}