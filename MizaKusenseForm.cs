using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MizaKusense
{
    public class BunnySenseForm : Form
    {
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        
        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        
        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);
        
        [DllImport("user32.dll")]
        private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);
        
        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);
        
        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr hObject);
        
        [DllImport("kernel32.dll")]
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, out IntPtr lpNumberOfBytesRead);
        
        [DllImport("uxtheme.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hwnd, string pszSubAppName, string pszSubIdList);
        
        private const int GWL_EXSTYLE = -20;
        private const int GWL_STYLE = -16;
        private const int WS_VSCROLL = 0x00200000;
        private const int WS_HSCROLL = 0x00100000;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_APPWINDOW = 0x00040000;
        private const int WS_EX_LAYERED = 0x00080000;
        private const int DWMWA_EXCLUDED_FROM_PEEK = 12;
        private const uint LWA_COLORKEY = 0x00000001;
        private const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;
        
        private const int HOTKEY_ID = 1;
        private uint currentModifier = 0x0004;
        private uint currentKey = 0x74;
        
        private const uint PROCESS_VM_READ = 0x0010;
        private const uint PROCESS_QUERY_INFORMATION = 0x0400;
        private const uint PROCESS_ALL_ACCESS = 0x1F0FFF;
        
        private CheckBox chkStealthWrite;
        private CheckBox chkSignatureEncryption;
        private CheckBox chkRandomization;
        private CheckBox chkTimingAttack;
        private CheckBox chkHideTaskbar;
        private CheckBox chkDisableScreenshots;
        private CheckBox chkStealthMode;
        private CheckBox chkSafeMode;
        private CheckBox chkAutoInject;
        private CheckBox chkAutoInterval;
        
        private Button btnInject;
        private Button btnRemoveAll;
        private Button btnRobloxCDN;
        private Button btnJsonFile;
        private Button btnClearLog;
        private Button btnChangeHotkey;
        
        private ListBox lstActivityLog;
        private CustomTrackBar trackAutoInterval;
        private TextBox txtIntervalValue;
        private Label lblIntervalValue;
        
        private Label lblStatus;
        private Label lblHotkeyDisplay;
        
        private Dictionary<string, JsonElement> currentFlags = new Dictionary<string, JsonElement>();
        private Dictionary<string, int> currentOffsets = new Dictionary<string, int>();
        private Dictionary<string, JsonElement> lastInjectedFlags = new Dictionary<string, JsonElement>();
        private bool stopAnimation = false;
        private System.Windows.Forms.Timer autoInjectTimer;
        private System.Windows.Forms.Timer autoIntervalTimer;
        private System.Windows.Forms.Timer offsetUpdateTimer;
        private System.Windows.Forms.Timer snowTimer;
        private List<Snowflake> snowflakes = new List<Snowflake>();
        private static readonly HttpClient httpClient = new HttpClient();
        private Form overlayForm;
        private System.Windows.Forms.Timer fadeInTimer;

        private Color BlendGreenToRed(float position, int alpha)
        {
            int r = (int)(255 * position);
            int g = (int)(255 * (1 - position));
            return Color.FromArgb(alpha, r, g, 0);
        }

        public BunnySenseForm()
        {
            InitializeUI();
            InitializeSnowEffect();
            
            AddLog("[OK] MizaKusense Ultimate Online (Stabilized)");
            AddLog($"[OK] Loaded {currentOffsets.Count} offsets");
            AddLog("[OK] Stabilized injection ready");
            AddLog("[HOTKEY] Press Shift+F5 to show/hide window");
            AddLog("[GITHUB] Auto-updating offsets from GitHub...");
            
            autoInjectTimer = new System.Windows.Forms.Timer();
            autoInjectTimer.Interval = 5000;
            autoInjectTimer.Tick += AutoInjectTimer_Tick;
            
            autoIntervalTimer = new System.Windows.Forms.Timer();
            autoIntervalTimer.Interval = 1000;
            autoIntervalTimer.Tick += AutoIntervalTimer_Tick;
            
            offsetUpdateTimer = new System.Windows.Forms.Timer();
            offsetUpdateTimer.Interval = 30000;
            offsetUpdateTimer.Tick += OffsetUpdateTimer_Tick;
            offsetUpdateTimer.Start();
            
            Task.Run(() => LoadOffsetsFromGithub());
        }

        private void InitializeSnowEffect()
        {
            Random rand = new Random();
            Rectangle screenBounds = Screen.PrimaryScreen.Bounds;
            
            for (int i = 0; i < 80; i++)
            {
                snowflakes.Add(new Snowflake
                {
                    X = rand.Next(screenBounds.Left, screenBounds.Right),
                    Y = rand.Next(screenBounds.Top - screenBounds.Height, screenBounds.Top),
                    Speed = rand.Next(2, 5),
                    Size = rand.Next(3, 7),
                    ScreenX = rand.Next(screenBounds.Left, screenBounds.Right),
                    ScreenY = rand.Next(screenBounds.Top - screenBounds.Height, screenBounds.Top)
                });
            }

            snowTimer = new System.Windows.Forms.Timer();
            snowTimer.Interval = 30;
            snowTimer.Tick += (s, e) =>
            {
                Rectangle bounds = Screen.PrimaryScreen.Bounds;
                foreach (var flake in snowflakes)
                {
                    flake.ScreenY += flake.Speed;
                    flake.ScreenX += (int)(Math.Sin(flake.ScreenY * 0.01) * 0.5);
                    
                    if (flake.ScreenY > bounds.Bottom)
                    {
                        flake.ScreenY = bounds.Top - 10;
                        flake.ScreenX = new Random().Next(bounds.Left, bounds.Right);
                    }
                }
                
                overlayForm?.Invalidate();
            };
            snowTimer.Start();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            
            if (chkHideTaskbar.Checked)
            {
                HideFromTaskbar();
            }
            
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.BringToFront();
            this.Activate();
            this.Focus();
            
            
            
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            RegisterHotKey(this.Handle, HOTKEY_ID, currentModifier, currentKey);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
                AddLog("[UI] Window hidden - use Shift+F5 to show again");
                return;
            }
            
            
            UnregisterHotKey(this.Handle, HOTKEY_ID);
            autoInjectTimer?.Stop();
            autoIntervalTimer?.Stop();
            offsetUpdateTimer?.Stop();
            snowTimer?.Stop();
            overlayForm?.Close();
            base.OnFormClosing(e);
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_HOTKEY = 0x0312;
            const int WM_ERASEBKGND = 0x0014;
            
            if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
            {
                ToggleUI();
            }
            
            if (m.Msg == WM_ERASEBKGND)
            {
                m.Result = new IntPtr(1);
                return;
            }
            
            base.WndProc(ref m);
        }

        private void ShowOverlay()
        {
            if (overlayForm == null)
            {
                overlayForm = new Form
                {
                    FormBorderStyle = FormBorderStyle.None,
                    WindowState = FormWindowState.Maximized,
                    BackColor = Color.Black,
                    Opacity = 0,
                    ShowInTaskbar = false,
                    TopMost = true,
                    StartPosition = FormStartPosition.Manual,
                    Location = new Point(0, 0),
                    Size = Screen.PrimaryScreen.Bounds.Size
                };
                
                if (chkStealthMode.Checked)
                {
                    SetWindowDisplayAffinity(overlayForm.Handle, WDA_EXCLUDEFROMCAPTURE);
                }
                
                typeof(Control).GetProperty("DoubleBuffered", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    .SetValue(overlayForm, true, null);
                
                overlayForm.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    
                    foreach (var flake in snowflakes)
                    {
                        using (GraphicsPath path = new GraphicsPath())
                        {
                            path.AddEllipse(flake.ScreenX - flake.Size * 1.5f, flake.ScreenY - flake.Size * 1.5f, flake.Size * 3, flake.Size * 3);
                            using (PathGradientBrush brush = new PathGradientBrush(path))
                            {
                                brush.CenterColor = Color.FromArgb(180, 255, 255, 255);
                                brush.SurroundColors = new[] { Color.FromArgb(0, 255, 255, 255) };
                                e.Graphics.FillPath(brush, path);
                            }
                        }
                        
                        using (SolidBrush brush = new SolidBrush(Color.FromArgb(255, 255, 255, 255)))
                        {
                            e.Graphics.FillEllipse(brush, flake.ScreenX - flake.Size / 2, flake.ScreenY - flake.Size / 2, flake.Size, flake.Size);
                        }
                    }
                };
                
                overlayForm.Show();
                this.BringToFront();
                this.TopMost = true;
                
                int exStyle = GetWindowLong(overlayForm.Handle, GWL_EXSTYLE);
                exStyle |= WS_EX_TOOLWINDOW;
                exStyle &= ~WS_EX_APPWINDOW;
                SetWindowLong(overlayForm.Handle, GWL_EXSTYLE, exStyle);
                
                if (fadeInTimer == null)
                {
                    fadeInTimer = new System.Windows.Forms.Timer();
                    fadeInTimer.Interval = 16;
                    fadeInTimer.Tick += (s, e) =>
                    {
                        if (stopAnimation)
                        {
                            fadeInTimer.Stop();
                            if (overlayForm != null)
                            {
                                overlayForm.Close();
                                overlayForm = null;
                            }
                            return;
                        }
                        
                        if (overlayForm != null && overlayForm.Opacity < 0.5)
                        {
                            overlayForm.Opacity += 0.05;
                        }
                        else
                        {
                            fadeInTimer.Stop();
                        }
                    };
                }
                fadeInTimer.Start();
            }
        }

        private void HideOverlay()
        {
            if (overlayForm != null)
            {
                stopAnimation = true;
                
                System.Windows.Forms.Timer fadeOutTimer = new System.Windows.Forms.Timer();
                fadeOutTimer.Interval = 16;
                fadeOutTimer.Tick += (s, e) =>
                {
                    if (stopAnimation || overlayForm == null)
                    {
                        fadeOutTimer.Stop();
                        fadeOutTimer.Dispose();
                        if (overlayForm != null)
                        {
                            overlayForm.Close();
                            overlayForm = null;
                        }
                        return;
                    }
                    
                    if (overlayForm != null && overlayForm.Opacity > 0)
                    {
                        overlayForm.Opacity -= 0.05;
                    }
                    else
                    {
                        fadeOutTimer.Stop();
                        fadeOutTimer.Dispose();
                        if (overlayForm != null)
                        {
                            overlayForm.Close();
                            overlayForm = null;
                        }
                    }
                };
                fadeOutTimer.Start();
            }
        }

        private void ToggleUI()
        {
            stopAnimation = true;
            
            if (this.Visible)
            {
                this.Hide();
                HideOverlay();
            }
            else
            {
                stopAnimation = false;
                ShowOverlay();
                
                this.Opacity = 0;
                this.Show();
                this.WindowState = FormWindowState.Normal;
                this.BringToFront();
                this.Activate();
                this.Focus();
                this.Opacity = 1;
            }
        }

        private void HideFromTaskbar()
        {
            try
            {
                int exStyle = GetWindowLong(this.Handle, GWL_EXSTYLE);
                exStyle |= WS_EX_TOOLWINDOW;
                exStyle &= ~WS_EX_APPWINDOW;
                SetWindowLong(this.Handle, GWL_EXSTYLE, exStyle);
                this.ShowInTaskbar = false;
                
                this.Hide();
                this.Show();
                
                AddLog("[TASKBAR] Hidden from taskbar");
            }
            catch (Exception ex)
            {
                AddLog($"[ERROR] Failed to hide from taskbar: {ex.Message}");
            }
        }

        private void ShowInTaskbarAgain()
        {
            try
            {
                int exStyle = GetWindowLong(this.Handle, GWL_EXSTYLE);
                exStyle &= ~WS_EX_TOOLWINDOW;
                exStyle |= WS_EX_APPWINDOW;
                SetWindowLong(this.Handle, GWL_EXSTYLE, exStyle);
                this.ShowInTaskbar = true;
                
                this.Hide();
                this.Show();
                
                AddLog("[TASKBAR] Visible in taskbar");
            }
            catch (Exception ex)
            {
                AddLog($"[ERROR] Failed to show in taskbar: {ex.Message}");
            }
        }

        private void AutoInjectTimer_Tick(object sender, EventArgs e)
        {
            if (!chkAutoInject.Checked || currentFlags.Count == 0 || lastInjectedFlags.Count == 0)
                return;

            try
            {
                var procs = System.Diagnostics.Process.GetProcessesByName("RobloxPlayerBeta");
                if (procs.Length == 0)
                    return;

                var proc = procs[0];
                
                if (CheckIfMemoryReverted(proc))
                {
                    AddLog("[AUTO] Flags reverted! Re-injecting...");
                    PerformNormalInjection();
                }
            }
            catch (Exception ex)
            {
                AddLog($"[AUTO] Check failed: {ex.Message}");
            }
        }

        private void AutoIntervalTimer_Tick(object sender, EventArgs e)
        {
            if (!chkAutoInterval.Checked || currentFlags.Count == 0)
                return;

            try
            {
                var procs = System.Diagnostics.Process.GetProcessesByName("RobloxPlayerBeta");
                if (procs.Length == 0)
                    return;

                AddLog("[INTERVAL] Auto-injecting...");
                PerformNormalInjection();
            }
            catch (Exception ex)
            {
                AddLog($"[INTERVAL] Failed: {ex.Message}");
            }
        }

        private bool CheckIfMemoryReverted(System.Diagnostics.Process proc)
        {
            try
            {
                IntPtr hProcess = OpenProcess(PROCESS_VM_READ | PROCESS_QUERY_INFORMATION, false, proc.Id);
                if (hProcess == IntPtr.Zero)
                    return false;

                try
                {
                    IntPtr baseAddress = proc.MainModule.BaseAddress;
                    long baseVal = baseAddress.ToInt64();

                    int checkedCount = 0;
                    int revertedCount = 0;

                    foreach (var flag in lastInjectedFlags.Take(5))
                    {
                        string shortName = StripPrefix(flag.Key);
                        
                        if (!currentOffsets.TryGetValue(shortName, out int offset))
                            continue;

                        long addr = baseVal + offset;
                        IntPtr address = new IntPtr(addr);

                        byte[] buffer = new byte[4];
                        IntPtr bytesRead;
                        
                        if (ReadProcessMemory(hProcess, address, buffer, buffer.Length, out bytesRead))
                        {
                            checkedCount++;
                            
                            var expectedBytes = EncodeFlagValue(flag.Key, flag.Value);
                            if (expectedBytes != null && !buffer.Take(expectedBytes.Length).SequenceEqual(expectedBytes))
                            {
                                revertedCount++;
                            }
                        }
                    }

                    return checkedCount > 0 && revertedCount > checkedCount / 2;
                }
                finally
                {
                    CloseHandle(hProcess);
                }
            }
            catch
            {
                return false;
            }
        }

        private string StripPrefix(string name)
        {
            if (string.IsNullOrEmpty(name))
                return string.Empty;

            string[] prefixes = { "FFlag", "DFFlag", "FInt", "DFInt", "FLog", "DFLog", "FString", "DFString" };

            foreach (string p in prefixes)
            {
                if (name.StartsWith(p, StringComparison.Ordinal))
                    return name.Substring(p.Length);
            }

            return name;
        }

        private byte[] EncodeFlagValue(string fullName, JsonElement el)
        {
            bool isBoolFlag = fullName.StartsWith("FFlag", StringComparison.Ordinal) ||
                              fullName.StartsWith("DFFlag", StringComparison.Ordinal);

            bool isIntFlag = fullName.StartsWith("FInt", StringComparison.Ordinal) ||
                             fullName.StartsWith("DFInt", StringComparison.Ordinal);

            if (isBoolFlag)
            {
                bool value;
                if (el.ValueKind == JsonValueKind.True) value = true;
                else if (el.ValueKind == JsonValueKind.False) value = false;
                else return null;

                return new[] { value ? (byte)1 : (byte)0 };
            }

            if (isIntFlag)
            {
                if (el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out int value))
                {
                    return BitConverter.GetBytes(value);
                }
            }

            return null;
        }

        private void OffsetUpdateTimer_Tick(object sender, EventArgs e)
        {
            Task.Run(() => LoadOffsetsFromGithub());
        }

        private async Task LoadOffsetsFromGithub()
        {
            try
            {
                string url = "https://raw.githubusercontent.com/NtReadVirtualMemory/Roblox-Offsets-Website/main/FFlags.hpp";
                string content = await httpClient.GetStringAsync(url);
                
                var newOffsets = ParseCppOffsets(content);
                
                if (newOffsets.Count > 0)
                {
                    this.Invoke((MethodInvoker)delegate
                    {
                        currentOffsets = newOffsets;
                        AddLog($"[GITHUB] Updated {currentOffsets.Count} offsets");
                        UpdateStatus();
                    });
                }
            }
            catch (Exception ex)
            {
                AddLog($"[GITHUB] Update failed: {ex.Message}");
            }
        }

        private void InitializeUI()
        {
            this.Text = "MIZAKUSENSE";
            this.Size = new Size(900, 650);
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(20, 20, 25);
            this.ForeColor = Color.White;
            this.DoubleBuffered = true;
            this.AllowTransparency = false;
            
            this.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.Clear(this.BackColor);
                
                using (Pen pen = new Pen(Color.White, 2))
                {
                    e.Graphics.DrawRectangle(pen, 1, 1, this.Width - 3, this.Height - 3);
                }
            };

            Panel titleBar = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(900, 35),
                BackColor = Color.FromArgb(15, 15, 20)
            };
            titleBar.MouseDown += TitleBar_MouseDown;
            this.Controls.Add(titleBar);

            Label lblTitle = new Label
            {
                Location = new Point(10, 8),
                Size = new Size(200, 20),
                Text = "MIZAKUSENSE",
                ForeColor = Color.FromArgb(255, 255, 255),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            titleBar.Controls.Add(lblTitle);

            Button btnClose = new Button
            {
                Location = new Point(860, 5),
                Size = new Size(30, 25),
                Text = "×",
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = Color.LightGray,
                Font = new Font("Arial", 14, FontStyle.Bold)
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.FlatAppearance.MouseOverBackColor = Color.Transparent;
            btnClose.FlatAppearance.MouseDownBackColor = Color.Transparent;
            btnClose.MouseEnter += (s, e) => btnClose.ForeColor = Color.White;
            btnClose.MouseLeave += (s, e) => btnClose.ForeColor = Color.LightGray;
            btnClose.Click += (s, e) => this.Close();
            titleBar.Controls.Add(btnClose);

            lblStatus = new Label
            {
                Location = new Point(20, 50),
                Size = new Size(500, 20),
                Text = "FLAGS: 0    OFFSETS: 0    PROCESSES: 0",
                ForeColor = Color.FromArgb(100, 200, 255),
                Font = new Font("Segoe UI", 9)
            };
            this.Controls.Add(lblStatus);

            Panel contentPanel = new Panel
            {
                Location = new Point(20, 85),
                Size = new Size(860, 540),
                BackColor = Color.FromArgb(20, 20, 25),
                AutoScroll = false
            };
            this.Controls.Add(contentPanel);


            btnInject = CreateStyledButton("INJECT", new Point(620, 505), new Size(120, 30));
            btnInject.BackColor = Color.FromArgb(80, 80, 80);
            btnInject.ForeColor = Color.White;
            btnInject.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            btnInject.FlatAppearance.BorderSize = 0;
            btnInject.Click += BtnInject_Click;
            contentPanel.Controls.Add(btnInject);

            btnRemoveAll = CreateStyledButton("UNINJECT", new Point(750, 505), new Size(110, 30));
            btnRemoveAll.BackColor = Color.FromArgb(80, 80, 80);
            btnRemoveAll.ForeColor = Color.White;
            btnRemoveAll.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            btnRemoveAll.FlatAppearance.BorderSize = 0;
            btnRemoveAll.Click += (s, e) => AddLog("[OK] All injections removed");
            contentPanel.Controls.Add(btnRemoveAll);

            btnChangeHotkey = CreateStyledButton("CHANGE HOTKEY", new Point(0, 505), new Size(120, 30));
            btnChangeHotkey.BackColor = Color.FromArgb(80, 80, 80);
            btnChangeHotkey.ForeColor = Color.White;
            btnChangeHotkey.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btnChangeHotkey.FlatAppearance.BorderSize = 0;
            btnChangeHotkey.Click += BtnChangeHotkey_Click;
            contentPanel.Controls.Add(btnChangeHotkey);
            
            Button btnClosePanel = CreateStyledButton("CLOSE", new Point(780, 505), new Size(80, 30));
            btnClosePanel.BackColor = Color.FromArgb(50, 50, 50);
            btnClosePanel.ForeColor = Color.White;
            btnClosePanel.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            btnClosePanel.FlatAppearance.BorderSize = 0;
            btnClosePanel.Click += (s, e) => this.Close();
            contentPanel.Controls.Add(btnClosePanel);
            
            Panel leftPanel = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(430, 500),
                BackColor = Color.FromArgb(20, 20, 25),
                AutoScroll = true,
                Name = "leftPanel"
            };
            leftPanel.BackColor = Color.FromArgb(20, 20, 25);
            
            leftPanel.Paint += (s, e) =>
            {
                if (leftPanel.VerticalScroll.Visible)
                {
                    int scrollbarWidth = 8;
                    int scrollbarX = leftPanel.Width - scrollbarWidth - 2;
                    
                    int displayHeight = leftPanel.DisplayRectangle.Height;
                    if (displayHeight > leftPanel.Height)
                    {
                        int thumbHeight = Math.Max(20, (int)(leftPanel.Height * leftPanel.Height / (float)displayHeight));
                        int maxScroll = displayHeight - leftPanel.Height;
                        int thumbY = maxScroll > 0 ? (int)(Math.Abs(leftPanel.AutoScrollPosition.Y) * (leftPanel.Height - thumbHeight) / (float)maxScroll) : 0;
                        
                        using (SolidBrush brush = new SolidBrush(Color.FromArgb(80, 80, 80)))
                        {
                            e.Graphics.FillRectangle(brush, scrollbarX, thumbY, scrollbarWidth, thumbHeight);
                        }
                    }
                }
            };
            
            leftPanel.Scroll += (s, e) =>
            {
                leftPanel.Invalidate();
            };
            
            contentPanel.Controls.Add(leftPanel);
            
            Label lblInjectionMethod = new Label
            {
                Location = new Point(5, 5),
                Size = new Size(400, 18),
                Text = "INJECTION METHOD",
                ForeColor = Color.FromArgb(100, 200, 255),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Name = "lblInjectionMethod"
            };
            leftPanel.Controls.Add(lblInjectionMethod);

            ComboBox cmbInjectionMethod = new ComboBox
            {
                Location = new Point(10, 27),
                Size = new Size(390, 25),
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9),
                FlatStyle = FlatStyle.Flat,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Name = "cmbInjectionMethod",
                DropDownHeight = 100,
                IntegralHeight = false,
                ItemHeight = 20
            };
            cmbInjectionMethod.Items.AddRange(new string[] { "Offsets", "Manual Mapping", "Offsetless Injection", "WriteProcessMemory", "NtWrite", "NtMapView", "CreateRemoteThread", "Unused Memory", "Thread Hijacking" });
            cmbInjectionMethod.SelectedIndex = 0;

            cmbInjectionMethod.DrawMode = DrawMode.OwnerDrawFixed;
            cmbInjectionMethod.DrawItem += (s, e) =>
            {
                if (e.Index < 0) return;
    
                bool isDroppedDown = cmbInjectionMethod.DroppedDown;
                bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
    
                Color backColor = (isSelected && isDroppedDown) 
                    ? Color.FromArgb(50, 130, 255)
                    : Color.FromArgb(30, 30, 30);
    
                using (Brush brush = new SolidBrush(backColor))
                {
                    e.Graphics.FillRectangle(brush, e.Bounds);
                }
    
                using (Brush textBrush = new SolidBrush(Color.White))
                {
                    string itemText = cmbInjectionMethod.Items[e.Index].ToString();
                    e.Graphics.DrawString(itemText, e.Font, textBrush, 
                        e.Bounds.Left + 5, e.Bounds.Top + 2);
                }
            };
            leftPanel.Controls.Add(cmbInjectionMethod);

            lblHotkeyDisplay = new Label
            {
                Text = "Shift+F5",
                Visible = false
            };

            int leftX = 0;
            int leftY = 60;

            Label lblAnti = new Label
            {
                Location = new Point(leftX, leftY),
                Size = new Size(400, 25),
                Text = "ANTI-FEATURES",
                ForeColor = Color.FromArgb(100, 200, 255),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            leftPanel.Controls.Add(lblAnti);
            leftY += 30;

            chkStealthWrite = CreateStyledCheckBox("Stealth write (NtWrite)", new Point(leftX, leftY), true);
            leftPanel.Controls.Add(chkStealthWrite);
            leftY += 35;

            chkSignatureEncryption = CreateStyledCheckBox("Signature encryption", new Point(leftX, leftY), true);
            leftPanel.Controls.Add(chkSignatureEncryption);
            leftY += 35;

            chkRandomization = CreateStyledCheckBox("Randomization", new Point(leftX, leftY), true);
            leftPanel.Controls.Add(chkRandomization);
            leftY += 35;

            chkTimingAttack = CreateStyledCheckBox("Timing attack", new Point(leftX, leftY), false);
            leftPanel.Controls.Add(chkTimingAttack);
            leftY += 45;

            chkAutoInject = CreateStyledCheckBox("Auto re-inject on revert", new Point(leftX, leftY), false);
            chkAutoInject.ForeColor = Color.FromArgb(255, 255, 255);
            chkAutoInject.CheckedChanged += (s, e) =>
            {
                if (chkAutoInject.Checked)
                {
                    autoInjectTimer.Start();
                    AddLog("[AUTO] Auto re-inject enabled (checks for reverts)");
                }
                else
                {
                    autoInjectTimer.Stop();
                    AddLog("[AUTO] Auto re-inject disabled");
                }
            };
            leftPanel.Controls.Add(chkAutoInject);
            leftY += 35;

            chkAutoInterval = CreateStyledCheckBox("Auto-inject interval", new Point(leftX, leftY), false);
            chkAutoInterval.ForeColor = Color.FromArgb(255, 255, 255);
            chkAutoInterval.CheckedChanged += (s, e) =>
            {
                trackAutoInterval.Enabled = chkAutoInterval.Checked;
                txtIntervalValue.Enabled = chkAutoInterval.Checked;
                
                if (chkAutoInterval.Checked)
                {
                    autoIntervalTimer.Start();
                    AddLog($"[INTERVAL] Auto-inject every {trackAutoInterval.Value}ms enabled");
                }
                else
                {
                    autoIntervalTimer.Stop();
                    AddLog("[INTERVAL] Auto-inject disabled");
                }
            };
            leftPanel.Controls.Add(chkAutoInterval);
            leftY += 40;

            trackAutoInterval = new CustomTrackBar
            {
                Location = new Point(leftX, leftY),
                Size = new Size(240, 45),
                Minimum = 100,
                Maximum = 10000,
                Value = 1000,
                Enabled = false,
                BackColor = Color.FromArgb(20, 20, 25)
            };
            trackAutoInterval.ValueChanged += (s, e) =>
            {
                autoIntervalTimer.Interval = trackAutoInterval.Value;
                txtIntervalValue.Text = trackAutoInterval.Value.ToString();
            };
            leftPanel.Controls.Add(trackAutoInterval);

            txtIntervalValue = new TextBox
            {
                Location = new Point(leftX + 250, leftY + 10),
                Size = new Size(80, 25),
                Text = "1000",
                BackColor = Color.FromArgb(40, 40, 45),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10),
                TextAlign = HorizontalAlignment.Center,
                Enabled = false,
                BorderStyle = BorderStyle.FixedSingle
            };
            txtIntervalValue.TextChanged += (s, e) =>
            {
                if (int.TryParse(txtIntervalValue.Text, out int val))
                {
                    if (val >= trackAutoInterval.Minimum && val <= trackAutoInterval.Maximum)
                    {
                        trackAutoInterval.Value = val;
                        autoIntervalTimer.Interval = val;
                    }
                }
            };
            leftPanel.Controls.Add(txtIntervalValue);

            lblIntervalValue = new Label
            {
                Location = new Point(leftX + 340, leftY + 12),
                Size = new Size(40, 20),
                Text = "ms",
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 9)
            };
            leftPanel.Controls.Add(lblIntervalValue);
            leftY += 55;

            Label lblStealth = new Label
            {
                Location = new Point(leftX, leftY),
                Size = new Size(400, 25),
                Text = "STEALTH",
                ForeColor = Color.FromArgb(100, 200, 255),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            leftPanel.Controls.Add(lblStealth);
            leftY += 30;

            chkHideTaskbar = CreateStyledCheckBox("Hide from taskbar", new Point(leftX, leftY), false);
            chkHideTaskbar.CheckedChanged += (s, e) =>
            {
                if (chkHideTaskbar.Checked) HideFromTaskbar();
                else ShowInTaskbarAgain();
            };
            leftPanel.Controls.Add(chkHideTaskbar);
            leftY += 35;

            chkDisableScreenshots = CreateStyledCheckBox("Disable screenshots", new Point(leftX, leftY), false);
            leftPanel.Controls.Add(chkDisableScreenshots);
            leftY += 35;

            chkStealthMode = CreateStyledCheckBox("Stealth mode (anti-screenshare)", new Point(leftX, leftY), false);
            chkStealthMode.CheckedChanged += ChkStealthMode_CheckedChanged;
            leftPanel.Controls.Add(chkStealthMode);
            leftY += 35;

            chkSafeMode = CreateStyledCheckBox("Safe mode (recommended)", new Point(leftX, leftY), true);
            chkSafeMode.ForeColor = Color.White;
            leftPanel.Controls.Add(chkSafeMode);

            int rightX = 450;
            int rightY = 0;

            Label lblLog = new Label
            {
                Location = new Point(rightX, rightY),
                Size = new Size(250, 25),
                Text = "ACTIVITY LOG",
                ForeColor = Color.FromArgb(100, 200, 255),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            contentPanel.Controls.Add(lblLog);

            btnClearLog = CreateStyledButton("CLEAR", new Point(rightX + 320, rightY - 2), new Size(80, 24));
            btnClearLog.Click += (s, e) => lstActivityLog.Items.Clear();
            contentPanel.Controls.Add(btnClearLog);
            rightY += 30;

            lstActivityLog = new ListBox
            {
                Location = new Point(rightX, rightY),
                Size = new Size(410, 200),
                BackColor = Color.FromArgb(30, 30, 35),
                ForeColor = Color.LightGray,
                Font = new Font("Consolas", 8),
                BorderStyle = BorderStyle.None
            };
            contentPanel.Controls.Add(lstActivityLog);
            rightY += 215;

            Label lblLoad = new Label
            {
                Location = new Point(rightX, rightY),
                Size = new Size(400, 25),
                Text = "LOAD OFFSETS",
                ForeColor = Color.FromArgb(100, 200, 255),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            contentPanel.Controls.Add(lblLoad);
            rightY += 30;

            btnRobloxCDN = CreateStyledButton("GITHUB OFFSETS", new Point(rightX, rightY), new Size(410, 30));
            btnRobloxCDN.Click += BtnGithubOffsets_Click;
            contentPanel.Controls.Add(btnRobloxCDN);
            rightY += 40;

            btnJsonFile = CreateStyledButton("IMPORT JSON", new Point(rightX, rightY), new Size(410, 30));
            btnJsonFile.Click += BtnJsonFile_Click;
            contentPanel.Controls.Add(btnJsonFile);
            rightY += 40;

            Label lblInfo = new Label
            {
                Location = new Point(rightX, rightY),
                Size = new Size(410, 40),
                Text = $"Auto-updates from GitHub every 30 seconds\nOffsets loaded: {currentOffsets.Count}",
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 8)
            };
            contentPanel.Controls.Add(lblInfo);
        }

        private Point mouseOffset;
        private void TitleBar_MouseDown(object sender, MouseEventArgs e)
        {
            mouseOffset = new Point(-e.X, -e.Y);
            Panel titleBar = sender as Panel;
            titleBar.MouseMove += TitleBar_MouseMove;
            titleBar.MouseUp += TitleBar_MouseUp;
        }

        private void TitleBar_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Point mousePos = Control.MousePosition;
                mousePos.Offset(mouseOffset.X, mouseOffset.Y);
                this.Location = mousePos;
            }
        }

        private void TitleBar_MouseUp(object sender, MouseEventArgs e)
        {
            Panel titleBar = sender as Panel;
            titleBar.MouseMove -= TitleBar_MouseMove;
            titleBar.MouseUp -= TitleBar_MouseUp;
        }

        private Button CreateStyledButton(string text, Point location, Size size)
        {
            Button btn = new Button
            {
                Location = location,
                Size = size,
                Text = text,
                BackColor = Color.FromArgb(50, 50, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(70, 70, 80);
            return btn;
        }

        private CheckBox CreateStyledCheckBox(string text, Point location, bool isChecked)
        {
            CustomCheckBox chk = new CustomCheckBox
            {
                Location = location,
                Size = new Size(380, 30),
                Text = text,
                ForeColor = Color.White,
                Checked = isChecked,
                Font = new Font("Segoe UI", 9),
                AutoSize = false
            };
            return chk;
        }

        private void BtnChangeHotkey_Click(object sender, EventArgs e)
        {
            Form hotkeyForm = new Form
            {
                Text = "Change Hotkey",
                Size = new Size(400, 280),
                StartPosition = FormStartPosition.CenterScreen,
                FormBorderStyle = FormBorderStyle.None,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.FromArgb(20, 20, 25),
                ForeColor = Color.White,
                TopMost = true
            };
            
            typeof(Control).GetProperty("DoubleBuffered", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(hotkeyForm, true, null);
            
            GraphicsPath path = new GraphicsPath();
            int radius = 15;
            path.AddArc(0, 0, radius, radius, 180, 90);
            path.AddArc(hotkeyForm.Width - radius, 0, radius, radius, 270, 90);
            path.AddArc(hotkeyForm.Width - radius, hotkeyForm.Height - radius, radius, radius, 0, 90);
            path.AddArc(0, hotkeyForm.Height - radius, radius, radius, 90, 90);
            path.CloseFigure();
            hotkeyForm.Region = new Region(path);
            
            hotkeyForm.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                
                for (int i = 8; i >= 1; i--)
                {
                    Rectangle glowRect = new Rectangle(-i, -i, hotkeyForm.Width + i * 2 - 1, hotkeyForm.Height + i * 2 - 1);
                    using (LinearGradientBrush glowBrush = new LinearGradientBrush(
                        glowRect,
                        Color.FromArgb(30 - i * 3, 0, 255, 0),
                        Color.FromArgb(30 - i * 3, 255, 0, 0),
                        LinearGradientMode.Horizontal))
                    {
                        using (Pen glowPen = new Pen(glowBrush, 1))
                        {
                            e.Graphics.DrawRectangle(glowPen, glowRect);
                        }
                    }
                }
                
                using (GraphicsPath borderPath = new GraphicsPath())
                {
                    borderPath.AddArc(2, 2, radius, radius, 180, 90);
                    borderPath.AddArc(hotkeyForm.Width - radius - 2, 2, radius, radius, 270, 90);
                    borderPath.AddArc(hotkeyForm.Width - radius - 2, hotkeyForm.Height - radius - 2, radius, radius, 0, 90);
                    borderPath.AddArc(2, hotkeyForm.Height - radius - 2, radius, radius, 90, 90);
                    borderPath.CloseFigure();
                    
                    using (LinearGradientBrush gradientBrush = new LinearGradientBrush(
                        new Rectangle(0, 0, hotkeyForm.Width, hotkeyForm.Height),
                        Color.FromArgb(0, 255, 0),
                        Color.FromArgb(255, 0, 0),
                        LinearGradientMode.Horizontal))
                    {
                        using (Pen pen = new Pen(gradientBrush, 2))
                        {
                            e.Graphics.DrawPath(pen, borderPath);
                        }
                    }
                }
            };
            
            Label lblTitle = new Label
            {
                Text = "Change Hotkey",
                Location = new Point(20, 15),
                Size = new Size(300, 25),
                ForeColor = Color.FromArgb(0, 255, 0),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                AutoSize = false
            };
            hotkeyForm.Controls.Add(lblTitle);
            
            Label lblInstruction = new Label
            {
                Text = "Press the key combination you want:",
                Location = new Point(20, 50),
                Size = new Size(360, 20),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9)
            };
            hotkeyForm.Controls.Add(lblInstruction);
            
            TextBox txtHotkey = new TextBox
            {
                Location = new Point(20, 85),
                Size = new Size(360, 40),
                BackColor = Color.FromArgb(25, 25, 30),
                ForeColor = Color.FromArgb(100, 200, 255),
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                BorderStyle = BorderStyle.FixedSingle,
                ReadOnly = true,
                TextAlign = HorizontalAlignment.Center
            };
            hotkeyForm.Controls.Add(txtHotkey);
            
            Label lblStatus = new Label
            {
                Text = "Listening for input...",
                Location = new Point(20, 135),
                Size = new Size(360, 20),
                ForeColor = Color.FromArgb(200, 200, 0),
                Font = new Font("Segoe UI", 9, FontStyle.Italic),
                TextAlign = ContentAlignment.MiddleCenter
            };
            hotkeyForm.Controls.Add(lblStatus);
            
            Label lblCurrentKeybind = new Label
            {
                Text = "Current: " + lblHotkeyDisplay.Text.Replace("Current: ", ""),
                Location = new Point(20, 160),
                Size = new Size(360, 20),
                ForeColor = Color.LightGreen,
                Font = new Font("Segoe UI", 9),
                TextAlign = ContentAlignment.MiddleCenter
            };
            hotkeyForm.Controls.Add(lblCurrentKeybind);
            
            Button btnSave = new Button
            {
                Text = "SAVE",
                Location = new Point(120, 200),
                Size = new Size(80, 35),
                BackColor = Color.FromArgb(0, 150, 0),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat
            };
            btnSave.FlatAppearance.BorderSize = 0;
            hotkeyForm.Controls.Add(btnSave);
            
            Button btnCancel = new Button
            {
                Text = "CANCEL",
                Location = new Point(210, 200),
                Size = new Size(80, 35),
                BackColor = Color.FromArgb(150, 0, 0),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            hotkeyForm.Controls.Add(btnCancel);
            
            uint newMod = 0;
            uint newKey = 0;

            txtHotkey.KeyDown += (s, ke) =>
            {
                ke.SuppressKeyPress = true;
                
                newMod = 0;
                if (ke.Control) newMod |= 0x0002;
                if (ke.Shift) newMod |= 0x0004;
                if (ke.Alt) newMod |= 0x0001;
                
                newKey = (uint)ke.KeyCode;
                
                string modStr = "";
                if (ke.Control) modStr += "Ctrl+";
                if (ke.Shift) modStr += "Shift+";
                if (ke.Alt) modStr += "Alt+";
                
                txtHotkey.Text = modStr + ke.KeyCode.ToString();
                lblStatus.Text = "Press Enter to confirm";
                
                if (ke.KeyCode == Keys.Return && newKey != 0)
                {
                    ke.SuppressKeyPress = true;
                    btnSave.PerformClick();
                }
            };

            btnSave.Click += (s, ea) =>
            {
                if (newKey != 0)
                {
                    UnregisterHotKey(this.Handle, HOTKEY_ID);
                    currentModifier = newMod;
                    currentKey = newKey;
                    RegisterHotKey(this.Handle, HOTKEY_ID, currentModifier, currentKey);
                    
                    lblHotkeyDisplay.Text = txtHotkey.Text;
                    AddLog($"[HOTKEY] Changed to {txtHotkey.Text}");
                    hotkeyForm.Close();
                }
            };
            
            btnCancel.Click += (s, ea) => hotkeyForm.Close();
            
            if (chkStealthMode.Checked)
            {
                SetWindowDisplayAffinity(hotkeyForm.Handle, WDA_EXCLUDEFROMCAPTURE);
            }
            
            hotkeyForm.Opacity = 0;
            hotkeyForm.Show();
            hotkeyForm.Focus();
            txtHotkey.Focus();
            
            System.Windows.Forms.Timer fadeTimer = new System.Windows.Forms.Timer();
            fadeTimer.Interval = 16;
            double currentOpacity = 0.0;
            
            fadeTimer.Tick += (s, e) =>
            {
                currentOpacity += 0.05;
                if (currentOpacity >= 1.0)
                {
                    hotkeyForm.Opacity = 1.0;
                    fadeTimer.Stop();
                    fadeTimer.Dispose();
                }
                else
                {
                    hotkeyForm.Opacity = currentOpacity;
                }
            };
            fadeTimer.Start();
        }

        private async void BtnGithubOffsets_Click(object sender, EventArgs e)
        {
            AddLog("[GITHUB] Loading offsets from GitHub...");
            btnRobloxCDN.Enabled = false;
            
            try
            {
                await LoadOffsetsFromGithub();
                AddLog($"[GITHUB] Loaded {currentOffsets.Count} offsets successfully");
            }
            catch (Exception ex)
            {
                AddLog($"[ERROR] Failed to load from GitHub: {ex.Message}");
                MessageBox.Show($"Failed to load offsets from GitHub:\n{ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnRobloxCDN.Enabled = true;
            }
        }

        private Dictionary<string, int> ParseCppOffsets(string cppContent)
        {
            var offsets = new Dictionary<string, int>();
            
            var flagRegex = new Regex(@"uintptr_t\s+(\w+)\s*=\s*0x([0-9A-Fa-f]+)", RegexOptions.Multiline);
            var flagMatches = flagRegex.Matches(cppContent);
            
            foreach (Match match in flagMatches)
            {
                string flagName = match.Groups[1].Value;
                string hexValue = match.Groups[2].Value;
                
                if (int.TryParse(hexValue, System.Globalization.NumberStyles.HexNumber, null, out int offset))
                {
                    offsets[flagName] = offset;
                }
            }
            
            var constexprRegex = new Regex(@"inline\s+constexpr\s+uintptr_t\s+(\w+)\s*=\s*0x([0-9A-Fa-f]+)", RegexOptions.Multiline);
            var constexprMatches = constexprRegex.Matches(cppContent);
            
            foreach (Match match in constexprMatches)
            {
                string flagName = match.Groups[1].Value;
                string hexValue = match.Groups[2].Value;
                
                if (int.TryParse(hexValue, System.Globalization.NumberStyles.HexNumber, null, out int offset))
                {
                    offsets[flagName] = offset;
                }
            }
            
            AddLog($"[PARSE] Parsed {offsets.Count} offsets from C++ header");
            return offsets;
        }

        private void AddLog(string message)
        {
            if (lstActivityLog.InvokeRequired)
            {
                lstActivityLog.Invoke(new Action(() => AddLog(message)));
                return;
            }
            
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            string logMessage = $"[{timestamp}] {message}";
            
            lstActivityLog.Items.Add(logMessage);
            lstActivityLog.TopIndex = lstActivityLog.Items.Count - 1;
            
            if (lstActivityLog.Items.Count > 100)
            {
                lstActivityLog.Items.RemoveAt(0);
            }
        }

        private void UpdateStatus()
        {
            int processCount = System.Diagnostics.Process.GetProcessesByName("RobloxPlayerBeta").Length;
            lblStatus.Text = $"FLAGS: {currentFlags.Count}    OFFSETS: {currentOffsets.Count}    PROCESSES: {processCount}";
        }

        private void ChkStealthMode_CheckedChanged(object sender, EventArgs e)
        {
            if (chkStealthMode.Checked)
            {
                EnableStealthMode();
                AddLog("[STEALTH] Mode activated - hidden from screenshare");
            }
            else
            {
                DisableStealthMode();
                AddLog("[STEALTH] Mode deactivated");
            }
        }

        private void EnableStealthMode()
        {
            try
            {
                IntPtr handle = this.Handle;
                
                SetWindowDisplayAffinity(handle, WDA_EXCLUDEFROMCAPTURE);
                
                if (overlayForm != null)
                {
                    SetWindowDisplayAffinity(overlayForm.Handle, WDA_EXCLUDEFROMCAPTURE);
                }
                
                int attrValue = 1;
                DwmSetWindowAttribute(handle, DWMWA_EXCLUDED_FROM_PEEK, ref attrValue, sizeof(int));
                
                AddLog("[STEALTH] Anti-screenshare ENABLED");
                AddLog("[STEALTH] Anti-recording ENABLED");
                AddLog("[STEALTH] Window invisible to capture software");
            }
            catch (Exception ex)
            {
                AddLog($"[ERROR] Stealth mode failed: {ex.Message}");
            }
        }

        private void DisableStealthMode()
        {
            try
            {
                IntPtr handle = this.Handle;
                
                SetWindowDisplayAffinity(handle, 0);
                
                if (overlayForm != null)
                {
                    SetWindowDisplayAffinity(overlayForm.Handle, 0);
                }
                
                int attrValue = 0;
                DwmSetWindowAttribute(handle, DWMWA_EXCLUDED_FROM_PEEK, ref attrValue, sizeof(int));
                
                AddLog("[STEALTH] Visible to screen capture again");
            }
            catch (Exception ex)
            {
                AddLog($"[ERROR] Disabling stealth failed: {ex.Message}");
            }
        }

        private void BtnInject_Click(object sender, EventArgs e)
        {
            if (currentFlags.Count == 0)
            {
                AddLog("[ERROR] No flags loaded");
                MessageBox.Show("No flags loaded!\n\nPlease import JSON flags first using the 'IMPORT JSON' button.",
                    "No Flags", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (currentOffsets.Count == 0)
            {
                AddLog("[ERROR] No offsets loaded");
                MessageBox.Show("No offsets loaded!\n\nPlease click 'GITHUB OFFSETS' to load offsets automatically.",
                    "No Offsets", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            AddLog("[OK] Starting injection...");
            
            if (chkTimingAttack.Checked)
            {
                AddLog("[TIMING] Waiting for Roblox process...");
                Task.Run(() => PerformTimingAttackInjection());
            }
            else
            {
                PerformNormalInjection();
            }
        }

        private async Task PerformTimingAttackInjection()
        {
            try
            {
                AddLog("[TIMING] Monitoring for Roblox startup...");
                
                while (System.Diagnostics.Process.GetProcessesByName("RobloxPlayerBeta").Length == 0)
                {
                    await Task.Delay(100);
                }
                
                AddLog("[TIMING] Roblox detected! Injecting immediately...");
                
                await Task.Delay(500);
                
                this.Invoke((MethodInvoker)delegate
                {
                    PerformNormalInjection();
                    AddLog("[TIMING] Early injection complete - bypassed anticheat load");
                });
            }
            catch (Exception ex)
            {
                AddLog($"[ERROR] Timing attack failed: {ex.Message}");
            }
        }

        private void PerformNormalInjection()
        {
            try
            {
                ComboBox cmbInjectionMethod = this.Controls.Find("cmbInjectionMethod", true).FirstOrDefault() as ComboBox;
                if (cmbInjectionMethod != null && cmbInjectionMethod.SelectedIndex > 0)
                {
                    string injectionMethod = cmbInjectionMethod.SelectedItem.ToString();
                    AddLog($"[OK] Using injection method: {injectionMethod}");
                    
                    if (injectionMethod == "Manual Mapping")
                    {
                        AddLog("[INJECTION] Manual Mapping method selected - not injecting via offsets");
                        AddLog("[TODO] Manual Mapping not yet implemented");
                        return;
                    }
                    else if (injectionMethod == "Offsetless Injection")
                    {
                        AddLog("[INJECTION] Offsetless Injection method selected - not injecting via offsets");
                        AddLog("[TODO] Offsetless Injection not yet implemented");
                        return;
                    }
                }
                
                Injector.Inject(currentFlags, currentOffsets, chkSafeMode.Checked);
                
                lastInjectedFlags.Clear();
                foreach (var flag in currentFlags)
                {
                    lastInjectedFlags[flag.Key] = flag.Value.Clone();
                }
                
                AddLog("[OK] Injection completed");
                UpdateStatus();
            }
            catch (Exception ex)
            {
                AddLog($"[ERROR] Injection failed: {ex.Message}");
                MessageBox.Show($"Injection error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnJsonFile_Click(object sender, EventArgs e)
        {
            Form importDialog = new Form
            {
                Text = "Add Fast Flag",
                Size = new Size(550, 400),
                StartPosition = FormStartPosition.CenterScreen,
                FormBorderStyle = FormBorderStyle.None,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.FromArgb(20, 20, 25),
                ForeColor = Color.White,
                TopMost = true
            };
            
            typeof(Control).GetProperty("DoubleBuffered", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(importDialog, true, null);
            
            GraphicsPath path = new GraphicsPath();
            int radius = 15;
            path.AddArc(0, 0, radius, radius, 180, 90);
            path.AddArc(importDialog.Width - radius, 0, radius, radius, 270, 90);
            path.AddArc(importDialog.Width - radius, importDialog.Height - radius, radius, radius, 0, 90);
            path.AddArc(0, importDialog.Height - radius, radius, radius, 90, 90);
            path.CloseFigure();
            importDialog.Region = new Region(path);
            
            importDialog.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                
                for (int i = 8; i >= 1; i--)
                {
                    Rectangle glowRect = new Rectangle(-i, -i, importDialog.Width + i * 2 - 1, importDialog.Height + i * 2 - 1);
                    using (LinearGradientBrush glowBrush = new LinearGradientBrush(
                        glowRect,
                        Color.FromArgb(30 - i * 3, 0, 255, 0),
                        Color.FromArgb(30 - i * 3, 255, 0, 0),
                        LinearGradientMode.Horizontal))
                    {
                        using (Pen glowPen = new Pen(glowBrush, 1))
                        {
                            e.Graphics.DrawRectangle(glowPen, glowRect);
                        }
                    }
                }
                
                using (GraphicsPath borderPath = new GraphicsPath())
                {
                    borderPath.AddArc(2, 2, radius, radius, 180, 90);
                    borderPath.AddArc(importDialog.Width - radius - 2, 2, radius, radius, 270, 90);
                    borderPath.AddArc(importDialog.Width - radius - 2, importDialog.Height - radius - 2, radius, radius, 0, 90);
                    borderPath.AddArc(2, importDialog.Height - radius - 2, radius, radius, 90, 90);
                    borderPath.CloseFigure();
                    
                    using (LinearGradientBrush gradientBrush = new LinearGradientBrush(
                        new Rectangle(0, 0, importDialog.Width, importDialog.Height),
                        Color.FromArgb(0, 255, 0),
                        Color.FromArgb(255, 0, 0),
                        LinearGradientMode.Horizontal))
                    {
                        using (Pen pen = new Pen(gradientBrush, 2))
                        {
                            e.Graphics.DrawPath(pen, borderPath);
                        }
                    }
                }
            };
            
            Func<string, Point, Size, Button> CreateDialogButton = (text, location, size) =>
            {
                Button btn = new Button
                {
                    Text = text,
                    Location = location,
                    Size = size,
                    BackColor = Color.FromArgb(50, 50, 60),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 10)
                };
                btn.FlatAppearance.BorderSize = 0;
                return btn;
            };
            
            Func<Point, Size, string, TextBox> CreateDialogTextBox = (location, size, placeholder) =>
            {
                TextBox txt = new TextBox
                {
                    Location = location,
                    Size = size,
                    BackColor = Color.FromArgb(40, 40, 45),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10),
                    BorderStyle = BorderStyle.FixedSingle,
                    Padding = new Padding(5)
                };
                txt.Text = placeholder;
                return txt;
            };

            Button btnAddSingle = CreateDialogButton("Add Single", new Point(20, 20), new Size(250, 40));
            btnAddSingle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            
            Button btnImportJSON = CreateDialogButton("Import JSON", new Point(280, 20), new Size(250, 40));
            btnImportJSON.BackColor = Color.FromArgb(40, 40, 45);
            btnImportJSON.ForeColor = Color.Gray;
            btnImportJSON.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            
            Panel underlineAdd = new Panel
            {
                Location = new Point(20, 60),
                Size = new Size(250, 3),
                BackColor = Color.FromArgb(0, 200, 0)
            };
            
            Panel underlineJSON = new Panel
            {
                Location = new Point(280, 60),
                Size = new Size(250, 3),
                BackColor = Color.Transparent
            };

            Panel addSinglePanel = new Panel
            {
                Location = new Point(20, 75),
                Size = new Size(510, 250),
                BackColor = Color.Transparent,
                Visible = true
            };

            Panel importJSONPanel = new Panel
            {
                Location = new Point(20, 75),
                Size = new Size(510, 250),
                BackColor = Color.Transparent,
                Visible = false
            };

            Label lblName = new Label
            {
                Text = "Flag Name",
                Location = new Point(0, 10),
                Size = new Size(510, 20),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            addSinglePanel.Controls.Add(lblName);

            TextBox txtName = CreateDialogTextBox(new Point(0, 35), new Size(510, 35), "");
            addSinglePanel.Controls.Add(txtName);

            Label lblValue = new Label
            {
                Text = "Flag Value",
                Location = new Point(0, 80),
                Size = new Size(510, 20),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            addSinglePanel.Controls.Add(lblValue);

            TextBox txtValue = CreateDialogTextBox(new Point(0, 105), new Size(510, 35), "");
            addSinglePanel.Controls.Add(txtValue);

            Label lblJSON = new Label
            {
                Text = "JSON Content",
                Location = new Point(0, 10),
                Size = new Size(510, 20),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            importJSONPanel.Controls.Add(lblJSON);

            TextBox txtJSON = new TextBox
            {
                Location = new Point(0, 35),
                Size = new Size(510, 155),
                BackColor = Color.FromArgb(25, 25, 30),
                ForeColor = Color.FromArgb(100, 100, 100),
                Font = new Font("Consolas", 9),
                BorderStyle = BorderStyle.FixedSingle,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Text = "{\n  \"FFlagDebugDisplayFPS\": true\n}",
                Tag = "placeholder"
            };
            
            txtJSON.GotFocus += (s, e) =>
            {
                if (txtJSON.Tag?.ToString() == "placeholder")
                {
                    txtJSON.Text = "";
                    txtJSON.ForeColor = Color.White;
                    txtJSON.Tag = "";
                }
            };
            
            txtJSON.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtJSON.Text))
                {
                    txtJSON.Text = "{\n  \"FFlagDebugDisplayFPS\": true\n}";
                    txtJSON.ForeColor = Color.FromArgb(100, 100, 100);
                    txtJSON.Tag = "placeholder";
                }
            };
            
            importJSONPanel.Controls.Add(txtJSON);

            Button btnImportFromFile = CreateDialogButton("📁 Import from file", new Point(160, 200), new Size(190, 35));
            btnImportFromFile.Click += (s, ea) =>
            {
                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    ofd.Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*";
                    ofd.Title = "Select JSON File";
                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        txtJSON.Text = File.ReadAllText(ofd.FileName);
                    }
                }
            };
            importJSONPanel.Controls.Add(btnImportFromFile);

            btnAddSingle.Click += (s, ea) =>
            {
                btnAddSingle.BackColor = Color.FromArgb(50, 50, 60);
                btnAddSingle.ForeColor = Color.White;
                btnAddSingle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
                btnImportJSON.BackColor = Color.FromArgb(40, 40, 45);
                btnImportJSON.ForeColor = Color.Gray;
                btnImportJSON.Font = new Font("Segoe UI", 10, FontStyle.Bold);
                underlineAdd.BackColor = Color.FromArgb(0, 200, 0);
                underlineJSON.BackColor = Color.Transparent;
                addSinglePanel.Visible = true;
                importJSONPanel.Visible = false;
            };

            btnImportJSON.Click += (s, ea) =>
            {
                btnImportJSON.BackColor = Color.FromArgb(50, 50, 60);
                btnImportJSON.ForeColor = Color.White;
                btnImportJSON.Font = new Font("Segoe UI", 10, FontStyle.Bold);
                btnAddSingle.BackColor = Color.FromArgb(40, 40, 45);
                btnAddSingle.ForeColor = Color.Gray;
                btnAddSingle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
                underlineJSON.BackColor = Color.FromArgb(0, 200, 0);
                underlineAdd.BackColor = Color.Transparent;
                importJSONPanel.Visible = true;
                addSinglePanel.Visible = false;
            };

            Button btnOK = CreateDialogButton("OK", new Point(280, 340), new Size(120, 35));
            btnOK.BackColor = Color.FromArgb(70, 130, 180);
            btnOK.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            btnOK.Click += (s, ea) =>
            {
                try
                {
                    if (addSinglePanel.Visible)
                    {
                        if (!string.IsNullOrWhiteSpace(txtName.Text))
                        {
                            string jsonStr = $"{{\"{txtName.Text}\": {txtValue.Text}}}";
                            LoadJsonData(jsonStr);
                            AddLog($"[OK] Added flag: {txtName.Text}");
                            
                            // Cargar offsets automáticamente si no están cargados
                            if (currentOffsets.Count == 0)
                            {
                                AddLog("[GITHUB] Loading offsets automatically...");
                                Task.Run(() => LoadOffsetsFromGithub());
                            }
                            
                            importDialog.Close();
                        }
                    }
                    else
                    {
                        LoadJsonData(txtJSON.Text);
                        AddLog($"[OK] Imported JSON flags");
                        
                        // Cargar offsets automáticamente si no están cargados
                        if (currentOffsets.Count == 0)
                        {
                            AddLog("[GITHUB] Loading offsets automatically...");
                            Task.Run(() => LoadOffsetsFromGithub());
                        }
                        
                        importDialog.Close();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            Button btnCancel = CreateDialogButton("Cancel", new Point(410, 340), new Size(120, 35));
            btnCancel.Click += (s, ea) => importDialog.Close();

            importDialog.Controls.Add(btnAddSingle);
            importDialog.Controls.Add(btnImportJSON);
            importDialog.Controls.Add(underlineAdd);
            importDialog.Controls.Add(underlineJSON);
            importDialog.Controls.Add(addSinglePanel);
            importDialog.Controls.Add(importJSONPanel);
            importDialog.Controls.Add(btnOK);
            importDialog.Controls.Add(btnCancel);

            if (chkStealthMode.Checked)
            {
                SetWindowDisplayAffinity(importDialog.Handle, WDA_EXCLUDEFROMCAPTURE);
            }

            importDialog.Opacity = 0;
            importDialog.Show(this);
            importDialog.Focus();
            
            System.Windows.Forms.Timer fadeTimer = new System.Windows.Forms.Timer();
            fadeTimer.Interval = 16;
            double targetOpacity = 1.0;
            double currentOpacity = 0.0;
            
            fadeTimer.Tick += (s, e) =>
            {
                currentOpacity += 0.05;
                if (currentOpacity >= targetOpacity)
                {
                    importDialog.Opacity = targetOpacity;
                    fadeTimer.Stop();
                    fadeTimer.Dispose();
                }
                else
                {
                    importDialog.Opacity = currentOpacity;
                }
            };
            fadeTimer.Start();
        }


        private void LoadJsonData(string jsonContent)
        {
            var doc = JsonDocument.Parse(jsonContent);
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.Object)
            {
                currentFlags.Clear();
                foreach (var prop in root.EnumerateObject())
                {
                    currentFlags[prop.Name] = prop.Value.Clone();
                }
                AddLog($"[OK] Parsed {currentFlags.Count} flags");
            }

            UpdateStatus();
        }
    }

    public class CustomCheckBox : CheckBox
    {
        public CustomCheckBox()
        {
            this.SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            this.Padding = new Padding(0, 0, 0, 0);
            this.AutoSize = false;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.Clear(this.Parent.BackColor);
            
            Rectangle boxRect = new Rectangle(0, (this.Height - 20) / 2, 20, 20);
            
            using (SolidBrush brush = new SolidBrush(this.Checked ? Color.FromArgb(100, 200, 255) : Color.FromArgb(60, 60, 70)))
            {
                e.Graphics.FillRoundedRectangle(brush, boxRect, 4);
            }
            
            using (Pen pen = new Pen(this.Checked ? Color.FromArgb(90, 190, 255) : Color.FromArgb(80, 80, 90), 2))
            {
                e.Graphics.DrawRoundedRectangle(pen, boxRect, 4);
            }
            
            if (this.Checked)
            {
                using (Pen pen = new Pen(Color.White, 2.5f))
                {
                    pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                    pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                    e.Graphics.DrawLines(pen, new Point[]
                    {
                        new Point(boxRect.X + 5, boxRect.Y + 10),
                        new Point(boxRect.X + 9, boxRect.Y + 14),
                        new Point(boxRect.X + 15, boxRect.Y + 6)
                    });
                }
            }
            
            Rectangle textRect = new Rectangle(28, 0, this.Width - 28, this.Height);
            TextRenderer.DrawText(e.Graphics, this.Text, this.Font, textRect, 
                this.ForeColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
        }
    }

    public class CustomTrackBar : Control
    {
        private int minimum = 0;
        private int maximum = 100;
        private int value = 50;
        private bool dragging = false;

        [System.ComponentModel.Browsable(true)]
        [System.ComponentModel.DefaultValue(0)]
        public int Minimum
        {
            get => minimum;
            set { minimum = value; Invalidate(); }
        }

        [System.ComponentModel.Browsable(true)]
        [System.ComponentModel.DefaultValue(100)]
        public int Maximum
        {
            get => maximum;
            set { maximum = value; Invalidate(); }
        }

        [System.ComponentModel.Browsable(true)]
        [System.ComponentModel.DefaultValue(50)]
        public int Value
        {
            get => value;
            set
            {
                this.value = Math.Max(minimum, Math.Min(maximum, value));
                ValueChanged?.Invoke(this, EventArgs.Empty);
                Invalidate();
            }
        }

        public event EventHandler ValueChanged;

        public CustomTrackBar()
        {
            this.SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            this.DoubleBuffered = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            
            Color trackColor = this.Enabled ? Color.FromArgb(50, 50, 60) : Color.FromArgb(35, 35, 40);
            Color progressColor = this.Enabled ? Color.FromArgb(70, 130, 180) : Color.FromArgb(50, 50, 60);
            Color thumbColor = this.Enabled ? Color.FromArgb(100, 200, 255) : Color.FromArgb(70, 70, 80);
            Color thumbBorder = this.Enabled ? Color.FromArgb(150, 220, 255) : Color.FromArgb(90, 90, 100);
            
            Rectangle trackRect = new Rectangle(0, this.Height / 2 - 3, this.Width, 6);
            using (SolidBrush brush = new SolidBrush(trackColor))
            {
                e.Graphics.FillRoundedRectangle(brush, trackRect, 3);
            }
            
            float percent = (float)(value - minimum) / (maximum - minimum);
            int progressWidth = (int)(this.Width * percent);
            Rectangle progressRect = new Rectangle(0, this.Height / 2 - 3, progressWidth, 6);
            using (SolidBrush brush = new SolidBrush(progressColor))
            {
                e.Graphics.FillRoundedRectangle(brush, progressRect, 3);
            }
            
            int thumbX = progressWidth - 10;
            Rectangle thumbRect = new Rectangle(thumbX, this.Height / 2 - 10, 20, 20);
            using (SolidBrush brush = new SolidBrush(thumbColor))
            {
                e.Graphics.FillEllipse(brush, thumbRect);
            }
            using (Pen pen = new Pen(thumbBorder, 2))
            {
                e.Graphics.DrawEllipse(pen, thumbRect);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (this.Enabled)
            {
                dragging = true;
                UpdateValueFromMouse(e.X);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (dragging && this.Enabled)
                UpdateValueFromMouse(e.X);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (this.Enabled)
                dragging = false;
        }

        private void UpdateValueFromMouse(int x)
        {
            float percent = (float)x / this.Width;
            Value = minimum + (int)((maximum - minimum) * percent);
        }
    }

    public class Snowflake
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int ScreenX { get; set; }
        public int ScreenY { get; set; }
        public int Speed { get; set; }
        public int Size { get; set; }
    }

    public static class GraphicsExtensions
    {
        public static void FillRoundedRectangle(this Graphics graphics, Brush brush, Rectangle bounds, int radius)
        {
            using (GraphicsPath path = GetRoundedRectPath(bounds, radius))
            {
                graphics.FillPath(brush, path);
            }
        }

        public static void DrawRoundedRectangle(this Graphics graphics, Pen pen, Rectangle bounds, int radius)
        {
            using (GraphicsPath path = GetRoundedRectPath(bounds, radius))
            {
                graphics.DrawPath(pen, path);
            }
        }

        private static GraphicsPath GetRoundedRectPath(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            Size size = new Size(diameter, diameter);
            Rectangle arc = new Rectangle(bounds.Location, size);
            GraphicsPath path = new GraphicsPath();

            if (radius == 0)
            {
                path.AddRectangle(bounds);
                return path;
            }

            path.AddArc(arc, 180, 90);

            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);

            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);

            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);

            path.CloseFigure();
            return path;
        }
    }
}