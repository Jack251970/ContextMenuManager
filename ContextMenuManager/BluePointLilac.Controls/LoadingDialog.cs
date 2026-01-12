/*
    Copyright (c) 2017 Marcin Szeniak (https://github.com/Klocman/)
    Apache License Version 2.0
*/

using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
using System.Timers;
using System.Windows.Forms;
using Timer = System.Timers.Timer;

namespace BluePointLilac.Controls
{
    public sealed partial class LoadingDialog : Form
    {
        private readonly Thread _workThread;
        private readonly LoadingDialogInterface _controller;
        private Point _offset;
        private ContentAlignment _ownerAlignment;
        private bool _startAutomatically;
        private NewProgressBar progressBar;
        private Panel panel1;
        private IContainer components;

        internal LoadingDialog(string title, Action<LoadingDialogInterface> action)
        {
            InitializeComponent();
            Text = title;
            ForeColor = DarkModeHelper.FormFore;
            BackColor = DarkModeHelper.FormBack;
            UseWaitCursor = true;

            _controller = new LoadingDialogInterface(this);
            _workThread = new Thread(() => ExecuteAction(action)) { Name = "LoadingDialogThread - " + title };
            
            // 监听主题变化
            DarkModeHelper.ThemeChanged += OnThemeChanged;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ClassStyle |= 0x20000; // csDropshadow
                return cp;
            }
        }

        public static Form DefaultOwner { get; set; }
        public Exception Error { get; private set; }
        internal ProgressBar ProgressBar => progressBar;

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (Owner != null)
            {
                Owner.Move += OwnerOnMove;
                Owner.Resize += OwnerOnMove;
                OwnerOnMove(this, e);
            }
            if (_startAutomatically) StartWork();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (Owner != null)
            {
                Owner.Move -= OwnerOnMove;
                Owner.Resize -= OwnerOnMove;
            }
            base.OnClosing(e);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _controller.Abort = true;
            base.OnFormClosed(e);
        }

        private void OwnerOnMove(object sender, EventArgs e)
        {
            if (Owner == null) return;

            var newPos = CalculatePosition();
            newPos.X += _offset.X;
            newPos.Y += _offset.Y;
            Location = newPos;
        }

        private Point CalculatePosition()
        {
            var pos = Point.Empty;

            // Vertical alignment
            if (_ownerAlignment.ToString().Contains("Middle"))
                pos.Y = Owner.Location.Y + Owner.Size.Height / 2 - Size.Height / 2;
            else if (_ownerAlignment.ToString().Contains("Bottom"))
                pos.Y = Owner.Location.Y + Owner.Size.Height - Size.Height;

            // Horizontal alignment  
            if (_ownerAlignment.ToString().Contains("Center"))
                pos.X = Owner.Location.X + Owner.Size.Width / 2 - Size.Width / 2;
            else if (_ownerAlignment.ToString().Contains("Right"))
                pos.X = Owner.Location.X + Owner.Size.Width - Size.Width;

            return pos;
        }

        public static LoadingDialog Show(Form owner, string title, Action<LoadingDialogInterface> action,
            Point offset = default(Point), ContentAlignment ownerAlignment = ContentAlignment.MiddleCenter)
        {
            owner = GetTopmostOwner(owner);
            var loadBar = CreateLoadingDialog(owner, title, action, offset, ownerAlignment);
            loadBar.Show(loadBar.Owner);
            return loadBar;
        }

        public static Exception ShowDialog(Form owner, string title, Action<LoadingDialogInterface> action,
            Point offset = default(Point), ContentAlignment ownerAlignment = ContentAlignment.MiddleCenter)
        {
            using (var loadBar = CreateLoadingDialog(owner, title, action, offset, ownerAlignment))
            {
                loadBar._startAutomatically = true;
                loadBar.ShowDialog(loadBar.Owner);
                return loadBar.Error;
            }
        }

        private static LoadingDialog CreateLoadingDialog(Form owner, string title,
            Action<LoadingDialogInterface> action, Point offset, ContentAlignment alignment)
        {
            return new LoadingDialog(title, action)
            {
                _offset = offset,
                _ownerAlignment = alignment,
                Owner = owner,
                StartPosition = FormStartPosition.Manual
            };
        }

        private static Form GetTopmostOwner(Form owner)
        {
            if (owner == null) owner = DefaultOwner;
            while (owner != null && owner.OwnedForms.Length > 0)
                owner = owner.OwnedForms[0];
            return owner;
        }

        public void StartWork() => _workThread.Start();
        private void panel1_Resize(object sender, EventArgs e) => Size = panel1.Size;

        private void ExecuteAction(Action<LoadingDialogInterface> action)
        {
            _controller.WaitTillDialogIsReady();
            try { action(_controller); }
            catch (Exception ex) { Error = ex; }
            _controller.CloseDialog();
        }
        
        // 主题变化事件处理
        private void OnThemeChanged(object sender, EventArgs e)
        {
            if (this.IsDisposed || this.Disposing) return;
            
            ForeColor = DarkModeHelper.FormFore;
            BackColor = DarkModeHelper.FormBack;
            
            if (panel1 != null)
            {
                panel1.BackColor = DarkModeHelper.FormBack;
            }
            
            Invalidate();
        }

        #region Windows Form Designer Code
        private void InitializeComponent()
        {
            this.components = new Container();
            progressBar = new NewProgressBar();
            panel1 = new Panel();
            panel1.SuspendLayout();
            SuspendLayout();

            // 修复进度条位置：移除 Dock 属性，设置固定位置和大小
            progressBar.Location = new Point(8, 10);
            progressBar.Size = new Size(391, 25);
            progressBar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            panel1.AutoSize = true;
            panel1.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            panel1.BorderStyle = BorderStyle.FixedSingle;
            panel1.Controls.Add(progressBar);
            panel1.Dock = DockStyle.Fill;
            panel1.MinimumSize = new Size(408, 45);
            panel1.Padding = new Padding(8, 10, 8, 10);
            panel1.Size = new Size(408, 45);
            panel1.Resize += panel1_Resize;

            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(408, 45);
            Controls.Add(panel1);
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;

            panel1.ResumeLayout(false);
            ResumeLayout(false);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DarkModeHelper.ThemeChanged -= OnThemeChanged;
                
                if (components != null)
                {
                    components.Dispose();
                }
                
                if (progressBar != null)
                    progressBar.Dispose();
                if (panel1 != null)
                    panel1.Dispose();
            }
            base.Dispose(disposing);
        }
        #endregion
    }

    public sealed class LoadingDialogInterface
    {
        private readonly Timer _updateTimer;
        private Action _lastProgressUpdate;

        internal LoadingDialogInterface(LoadingDialog dialog)
        {
            Dialog = dialog;
            _updateTimer = new Timer { Interval = 35, SynchronizingObject = Dialog };
            _updateTimer.Elapsed += (s, e) =>
            {
                var action = Interlocked.Exchange(ref _lastProgressUpdate, null);
                if (action != null) action();
            };
            _updateTimer.Start();
            dialog.Disposed += (s, e) => _updateTimer.Dispose();
        }

        public bool Abort { get; internal set; }
        private LoadingDialog Dialog { get; }

        public void CloseDialog()
        {
            _updateTimer.Dispose();
            SafeInvoke(() => { if (!Dialog.IsDisposed) Dialog.Close(); });
        }

        public void SetMaximum(int value) => SafeInvoke(() => UpdateProgressBar(pb => pb.Maximum = value));
        public void SetMinimum(int value) => SafeInvoke(() => UpdateProgressBar(pb => pb.Minimum = value));

        public void SetProgress(int value, string description = null, bool forceNoAnimation = false)
        {
            _lastProgressUpdate = () => UpdateProgressBar(pb => ApplyProgressValue(pb, value, forceNoAnimation));
        }

        public void SetTitle(string newTitle) => SafeInvoke(() => Dialog.Text = newTitle);

        internal void WaitTillDialogIsReady()
        {
            var notReady = true;
            while (notReady)
            {
                SafeInvoke(() => notReady = !Dialog.Visible);
                Thread.Sleep(10);
            }
        }

        private void ApplyProgressValue(ProgressBar pb, int value, bool forceNoAnimation)
        {
            try
            {
                if (pb.Value == value) return;

                if (value < pb.Minimum || value > pb.Maximum)
                    pb.Style = ProgressBarStyle.Marquee;
                else
                {
                    pb.Style = ProgressBarStyle.Blocks;
                    if (forceNoAnimation && value < pb.Maximum)
                        pb.Value = value + 1;
                    pb.Value = value;
                }
            }
            catch { pb.Style = ProgressBarStyle.Marquee; }
        }

        private void UpdateProgressBar(Action<ProgressBar> action)
        {
            var pb = Dialog.ProgressBar;
            if (pb != null && !pb.IsDisposed) action(pb);
        }

        private void SafeInvoke(Action action)
        {
            if (Dialog.IsDisposed || Dialog.Disposing) return;

            if (Dialog.InvokeRequired)
            {
                try { Dialog.Invoke(action); }
                catch { /* 忽略调用异常 */ }
            }
            else action();
        }
    }

    public class NewProgressBar : ProgressBar
    {
        public NewProgressBar()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            // 修复黑色背景问题：绘制背景色
            using (var brush = new SolidBrush(this.Parent.BackColor))
            {
                pevent.Graphics.FillRectangle(brush, pevent.ClipRectangle);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            const int inset = 1;
            const int cornerRadius = 6;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            // 先清除为父控件背景色，避免黑色背景
            using (var bgClearBrush = new SolidBrush(this.Parent.BackColor))
            {
                e.Graphics.FillRectangle(bgClearBrush, this.ClientRectangle);
            }

            Rectangle rect = new Rectangle(0, 0, this.Width, this.Height);

            // 绘制背景（圆角矩形，使用三色渐变）
            using (GraphicsPath bgPath = CreateRoundedRectanglePath(rect, cornerRadius))
            {
                // 背景三色渐变
                Color bgColor1 = Color.FromArgb(220, 220, 220);
                Color bgColor2 = Color.FromArgb(180, 180, 180);
                Color bgColor3 = Color.FromArgb(220, 220, 220);

                using (LinearGradientBrush bgBrush = new LinearGradientBrush(
                    new Point(0, rect.Top),
                    new Point(0, rect.Bottom),
                    bgColor1, bgColor3))
                {
                    // 设置背景三色渐变
                    ColorBlend bgColorBlend = new ColorBlend();
                    bgColorBlend.Colors = new Color[] { bgColor1, bgColor2, bgColor3 };
                    bgColorBlend.Positions = new float[] { 0f, 0.5f, 1f };
                    bgBrush.InterpolationColors = bgColorBlend;

                    e.Graphics.FillPath(bgBrush, bgPath);
                }
            }

            // 计算进度宽度
            int progressWidth = (int)((this.Width - 2 * inset) * ((double)this.Value / this.Maximum));

            if (progressWidth > 0)
            {
                Rectangle progressRect = new Rectangle(inset, inset, progressWidth, this.Height - 2 * inset);

                // 确保进度条至少有最小宽度来显示圆角
                if (progressWidth < cornerRadius * 2)
                {
                    progressRect.Width = Math.Max(progressWidth, cornerRadius);
                }

                // 创建前景三色橘色垂直渐变
                Color fgColor1 = Color.FromArgb(255, 195, 0);
                Color fgColor2 = Color.FromArgb(255, 140, 26);
                Color fgColor3 = Color.FromArgb(255, 195, 0);

                using (LinearGradientBrush fgBrush = new LinearGradientBrush(
                    new Point(0, progressRect.Top),
                    new Point(0, progressRect.Bottom),
                    fgColor1, fgColor3))
                {
                    // 设置前景三色渐变
                    ColorBlend fgColorBlend = new ColorBlend();
                    fgColorBlend.Colors = new Color[] { fgColor1, fgColor2, fgColor3 };
                    fgColorBlend.Positions = new float[] { 0f, 0.5f, 1f };
                    fgBrush.InterpolationColors = fgColorBlend;

                    // 绘制进度条（圆角矩形）
                    using (GraphicsPath progressPath = CreateRoundedRectanglePath(progressRect, cornerRadius - 1))
                    {
                        e.Graphics.FillPath(fgBrush, progressPath);
                    }
                }
            }
        }

        // 创建圆角矩形路径的辅助方法
        private GraphicsPath CreateRoundedRectanglePath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();

            if (radius <= 0)
            {
                path.AddRectangle(rect);
                return path;
            }

            // 确保半径不会超过矩形尺寸的一半
            radius = Math.Min(radius, Math.Min(rect.Width, rect.Height) / 2);

            int diameter = radius * 2;

            // 左上角
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);

            // 右上角
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);

            // 右下角
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);

            // 左下角
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);

            path.CloseFigure();
            return path;
        }
    }
}