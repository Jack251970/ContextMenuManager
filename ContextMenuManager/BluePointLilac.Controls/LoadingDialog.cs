/*
    Copyright (c) 2017 Marcin Szeniak (https://github.com/Klocman/)
    Apache License Version 2.0
*/

using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
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

        internal LoadingDialog(string title, Action<LoadingDialogInterface> action)
        {
            InitializeComponent();
            Text = title;
            ForeColor = DarkModeHelper.FormFore;
            BackColor = DarkModeHelper.FormBack;
            UseWaitCursor = true;

            _controller = new LoadingDialogInterface(this);
            _workThread = new Thread(() => ExecuteAction(action)) { Name = "LoadingDialogThread - " + title };

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
            var ownerAlignment = _ownerAlignment.ToString();

            if (ownerAlignment.Contains("Middle"))
                pos.Y = Owner.Location.Y + Owner.Size.Height / 2 - Size.Height / 2;
            else if (ownerAlignment.Contains("Bottom"))
                pos.Y = Owner.Location.Y + Owner.Size.Height - Size.Height;

            if (ownerAlignment.Contains("Center"))
                pos.X = Owner.Location.X + Owner.Size.Width / 2 - Size.Width / 2;
            else if (ownerAlignment.Contains("Right"))
                pos.X = Owner.Location.X + Owner.Size.Width - Size.Width;

            return pos;
        }

        public static LoadingDialog Show(Form owner, string title, Action<LoadingDialogInterface> action,
            Point offset = default, ContentAlignment ownerAlignment = ContentAlignment.MiddleCenter)
        {
            owner = GetTopmostOwner(owner);
            var loadBar = CreateLoadingDialog(owner, title, action, offset, ownerAlignment);
            loadBar.Show(loadBar.Owner);
            return loadBar;
        }

        public static Exception ShowDialog(Form owner, string title, Action<LoadingDialogInterface> action,
            Point offset = default, ContentAlignment ownerAlignment = ContentAlignment.MiddleCenter)
        {
            using var loadBar = CreateLoadingDialog(owner, title, action, offset, ownerAlignment);
            loadBar._startAutomatically = true;
            loadBar.ShowDialog(loadBar.Owner);
            return loadBar.Error;
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

        public void StartWork()
        {
            _workThread.Start();
        }

        private void panel1_Resize(object sender, EventArgs e)
        {
            Size = panel1.Size;
        }

        private void ExecuteAction(Action<LoadingDialogInterface> action)
        {
            _controller.WaitTillDialogIsReady();
            try { action(_controller); }
            catch (Exception ex) { Error = ex; }
            _controller.CloseDialog();
        }

        private void OnThemeChanged(object sender, EventArgs e)
        {
            if (IsDisposed || Disposing) return;
            ForeColor = DarkModeHelper.FormFore;
            BackColor = DarkModeHelper.FormBack;
            panel1.BackColor = DarkModeHelper.FormBack;
            Invalidate();
        }

        private void InitializeComponent()
        {
            progressBar = new NewProgressBar();
            panel1 = new Panel();
            panel1.SuspendLayout();
            SuspendLayout();

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
                progressBar?.Dispose();
                panel1?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    public sealed class LoadingDialogInterface
    {
        private readonly Timer _updateTimer = new() { Interval = 35 };
        private Action _lastProgressUpdate;

        internal LoadingDialogInterface(LoadingDialog dialog)
        {
            Dialog = dialog;
            _updateTimer.SynchronizingObject = Dialog;
            _updateTimer.Elapsed += (s, e) =>
                Interlocked.Exchange(ref _lastProgressUpdate, null)?.Invoke();
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

        public void SetMaximum(int value)
        {
            SafeInvoke(() => UpdateProgressBar(pb => pb.Maximum = value));
        }

        public void SetMinimum(int value)
        {
            SafeInvoke(() => UpdateProgressBar(pb => pb.Minimum = value));
        }

        public void SetProgress(int value, string description = null, bool forceNoAnimation = false)
        {
            _lastProgressUpdate = () => UpdateProgressBar(pb => ApplyProgressValue(pb, value, forceNoAnimation));
        }

        public void SetTitle(string newTitle)
        {
            SafeInvoke(() => Dialog.Text = newTitle);
        }

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

            DarkModeHelper.ThemeChanged += OnThemeChanged;
        }

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            using var brush = new SolidBrush(DarkModeHelper.FormBack);
            pevent.Graphics.FillRectangle(brush, pevent.ClipRectangle);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            const int inset = 1;
            const int cornerRadius = 6;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            using (var bgClearBrush = new SolidBrush(DarkModeHelper.FormBack))
                e.Graphics.FillRectangle(bgClearBrush, ClientRectangle);

            var rect = new Rectangle(0, 0, Width, Height);

            using (var bgPath = CreateRoundedRectanglePath(rect, cornerRadius))
            {
                Color bgColor1, bgColor2, bgColor3;

                if (DarkModeHelper.IsDarkTheme)
                {
                    bgColor1 = Color.FromArgb(80, 80, 80);
                    bgColor2 = Color.FromArgb(60, 60, 60);
                    bgColor3 = Color.FromArgb(80, 80, 80);
                }
                else
                {
                    bgColor1 = Color.FromArgb(220, 220, 220);
                    bgColor2 = Color.FromArgb(180, 180, 180);
                    bgColor3 = Color.FromArgb(220, 220, 220);
                }

                using var bgBrush = new LinearGradientBrush(
                    new Point(0, rect.Top), new Point(0, rect.Bottom), bgColor1, bgColor3);
                bgBrush.InterpolationColors = new ColorBlend
                {
                    Colors = new[] { bgColor1, bgColor2, bgColor3 },
                    Positions = new[] { 0f, 0.5f, 1f }
                };
                e.Graphics.FillPath(bgBrush, bgPath);
            }

            var progressWidth = (int)((Width - 2 * inset) * ((double)Value / Maximum));

            if (progressWidth > 0)
            {
                var progressRect = new Rectangle(inset, inset, progressWidth, Height - 2 * inset);
                if (progressWidth < cornerRadius * 2)
                    progressRect.Width = Math.Max(progressWidth, cornerRadius);

                var fgColor1 = Color.FromArgb(255, 195, 0);
                var fgColor2 = Color.FromArgb(255, 140, 26);
                var fgColor3 = Color.FromArgb(255, 195, 0);

                using var fgBrush = new LinearGradientBrush(
                    new Point(0, progressRect.Top), new Point(0, progressRect.Bottom), fgColor1, fgColor3);
                fgBrush.InterpolationColors = new ColorBlend
                {
                    Colors = new[] { fgColor1, fgColor2, fgColor3 },
                    Positions = new[] { 0f, 0.5f, 1f }
                };

                using var progressPath = CreateRoundedRectanglePath(progressRect, cornerRadius - 1);
                e.Graphics.FillPath(fgBrush, progressPath);
            }
        }

        private GraphicsPath CreateRoundedRectanglePath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            if (radius <= 0)
            {
                path.AddRectangle(rect);
                return path;
            }

            radius = Math.Min(radius, Math.Min(rect.Width, rect.Height) / 2);
            var diameter = radius * 2;

            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void OnThemeChanged(object sender, EventArgs e)
        {
            Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) DarkModeHelper.ThemeChanged -= OnThemeChanged;
            base.Dispose(disposing);
        }
    }
}