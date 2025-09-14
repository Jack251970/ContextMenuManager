/*
    Copyright (c) 2017 Marcin Szeniak (https://github.com/Klocman/)
    Apache License Version 2.0
*/

using System;
using System.ComponentModel;
using System.Drawing;
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

        internal LoadingDialog(string title, Action<LoadingDialogInterface> action)
        {
            InitializeComponent();

            Text = title;

            UseWaitCursor = true;

            _controller = new LoadingDialogInterface(this);

            _workThread = new Thread(() =>
            {
                _controller.WaitTillDialogIsReady();
                try
                {
                    action(_controller);
                }
                catch (Exception ex)
                {
                    Error = ex;
                }

                _controller.CloseDialog();
            })
            { Name = "LoadingDialogThread - " + title };
        }

        protected override CreateParams CreateParams
        {
            get
            {
                const int csDropshadow = 0x20000;
                var cp = base.CreateParams;
                cp.ClassStyle |= csDropshadow;
                return cp;
            }
        }

        public static Form DefaultOwner { get; set; }

        public Exception Error { get; private set; }

        internal ProgressBar ProgressBar => progressBar;

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if (Owner?.Icon != null)
                Icon = Owner.Icon;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            if (Owner != null)
            {
                Owner.Move += OwnerOnMove;
                Owner.Resize += OwnerOnMove;

                OwnerOnMove(this, e);
            }

            if (_startAutomatically)
                StartWork();
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

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            OwnerOnMove(this, e);
        }

        private void OwnerOnMove(object sender, EventArgs eventArgs)
        {
            if (Owner == null) return;

            var newPos = new Point();

            switch (_ownerAlignment)
            {
                case ContentAlignment.MiddleLeft:
                case ContentAlignment.MiddleCenter:
                case ContentAlignment.MiddleRight:
                    newPos.Y = Owner.Location.Y + Owner.Size.Height / 2 - Size.Height / 2;
                    break;

                case ContentAlignment.BottomLeft:
                case ContentAlignment.BottomCenter:
                case ContentAlignment.BottomRight:
                    newPos.Y = Owner.Location.Y + Owner.Size.Height - Size.Height;
                    break;

                default:
                    break;
            }
            switch (_ownerAlignment)
            {
                case ContentAlignment.TopCenter:
                case ContentAlignment.MiddleCenter:
                case ContentAlignment.BottomCenter:
                    newPos.X = Owner.Location.X + Owner.Size.Width / 2 - Size.Width / 2;
                    break;
                case ContentAlignment.TopRight:
                case ContentAlignment.MiddleRight:
                case ContentAlignment.BottomRight:
                    newPos.X = Owner.Location.X + Owner.Size.Width - Size.Width;
                    break;

                default:
                    break;
            }

            newPos.X += _offset.X;
            newPos.Y += _offset.Y;
            Location = newPos;
        }

        /// <summary>
        ///     Start the action on a separate thread and show a progress bar. When action completes the dialog is closed.
        /// </summary>
        /// <param name="owner">Parent form</param>
        /// <param name="title">Title of the window</param>
        /// <param name="action">Action to perform while displaying the dialog</param>
        /// <param name="offset">Offset from the alignment point</param>
        /// <param name="ownerAlignment">Alignment point to the parent</param>
        public static LoadingDialog Show(Form owner, string title, Action<LoadingDialogInterface> action,
            Point offset = default(Point), ContentAlignment ownerAlignment = ContentAlignment.MiddleCenter)
        {
            if (owner == null) owner = DefaultOwner;
            // Find the topmost form so clicking on forms above owner doesn't bring the loading form under the others
            while (owner != null && owner.OwnedForms.Length > 0) owner = owner.OwnedForms[0];

            var loadBar = new LoadingDialog(title, action)
            {
                _offset = offset,
                _ownerAlignment = ownerAlignment,
                Owner = owner,
                StartPosition = FormStartPosition.Manual,
                _startAutomatically = false,
            };

            loadBar.Show(loadBar.Owner);
            return loadBar;
        }

        /// <summary>
        ///     Start the action on a separate thread and show a progress bar.
        /// </summary>
        /// <param name="owner">Parent form</param>
        /// <param name="title">Title of the window</param>
        /// <param name="action">Action to perform while displaying the dialog</param>
        /// <param name="offset">Offset from the alignment point</param>
        /// <param name="ownerAlignment">Alignment point to the parent</param>
        public static Exception ShowDialog(Form owner, string title, Action<LoadingDialogInterface> action,
            Point offset = default(Point), ContentAlignment ownerAlignment = ContentAlignment.MiddleCenter)
        {
            using (var loadBar = new LoadingDialog(title, action)
            {
                _offset = offset,
                _ownerAlignment = ownerAlignment,
                Owner = owner ?? DefaultOwner,
                StartPosition = FormStartPosition.Manual,
                _startAutomatically = true
            })
            {
                loadBar.ShowDialog(loadBar.Owner);
                return loadBar.Error;
            }
        }

        public void StartWork()
        {
            _workThread.Start();
        }

        private void panel1_Resize(object sender, EventArgs e)
        {
            Size = panel1.Size;
        }
    }

    public sealed class LoadingDialogInterface
    {
        private readonly Timer _updateTimer;

        private Action _lastProgressUpdate;
        private Action _lastSubProgressUpdate;

        internal LoadingDialogInterface(LoadingDialog dialog)
        {
            Dialog = dialog;
            _updateTimer = new Timer
            {
                AutoReset = true,
                Interval = 35,
                SynchronizingObject = Dialog
            };
            _updateTimer.Elapsed += UpdateTimerOnElapsed;
            _updateTimer.Start();
            dialog.Disposed += (sender, args) => _updateTimer.Dispose();
        }

        private void UpdateTimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            var p = Interlocked.Exchange(ref _lastProgressUpdate, null);
            p?.Invoke();
            p = Interlocked.Exchange(ref _lastSubProgressUpdate, null);
            p?.Invoke();
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
            SetMaximumInt(value, Dialog.ProgressBar);
        }

        private void SetMaximumInt(int value, ProgressBar targetBar)
        {
            SafeInvoke(() =>
            {
                if (targetBar.Maximum == value) return;

                if (value < targetBar.Minimum)
                    targetBar.Style = ProgressBarStyle.Marquee;
                else
                {
                    try
                    {
                        targetBar.Maximum = value;
                    }
                    catch
                    {
                        targetBar.Style = ProgressBarStyle.Marquee;
                    }
                }
            });
        }

        public void SetMinimum(int value)
        {
            SetMinimumInt(value, Dialog.ProgressBar);
        }

        private void SetMinimumInt(int value, ProgressBar targetBar)
        {
            SafeInvoke(() =>
            {
                try
                {
                    targetBar.Minimum = value;
                }
                catch
                {
                    targetBar.Style = ProgressBarStyle.Marquee;
                }
            });
        }

        /// <summary>
        ///     Setting progress automatically changes the style from "Unknown time" (Marquee) to a normal progressbar.
        ///     If the value is invalid the style goes back to Marquee
        /// </summary>
        public void SetProgress(int value, string description = null, bool forceNoAnimation = false)
        {
            _lastProgressUpdate = () => SetProgressInt(value, description, Dialog.ProgressBar, forceNoAnimation);
        }

        private void SetProgressInt(int value, string description, ProgressBar targetBar, bool forceNoAnimation)
        {
            try
            {
                if (targetBar.Value == value)
                    return;

                var min = targetBar.Minimum;
                var max = targetBar.Maximum;
                if (min >= max || min > value || value > max)
                {
                    targetBar.Style = ProgressBarStyle.Marquee;
                }
                else
                {
                    targetBar.Style = ProgressBarStyle.Blocks;

                    if (value < min) value = min;
                    else if (value > max) value = max;

                    if (forceNoAnimation)
                    {
                        // Moving progress forward then back skips the animation
                        if (value == max)
                        {
                            targetBar.Maximum = value + 1;
                            targetBar.Value = value + 1;
                            targetBar.Maximum = value;
                        }
                        else
                        {
                            targetBar.Value = value + 1;
                        }
                    }

                    targetBar.Value = value;
                }
            }
            catch
            {
                targetBar.Style = ProgressBarStyle.Marquee;
            }
        }

        public void SetTitle(string newTitle)
        {
            SafeInvoke(() => { Dialog.Text = newTitle; });
        }

        /// <summary>
        ///     Block current thread until dialog is visible
        /// </summary>
        internal void WaitTillDialogIsReady()
        {
            var notReady = true;
            while (notReady)
            {
                SafeInvoke(() => notReady = !Dialog.Visible);
                Thread.Sleep(10);
            }
        }

        private void SafeInvoke(Action action)
        {
            Control obj = Dialog;

            if (action == null)
                throw new ArgumentNullException(nameof(action));
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            if (!obj.IsDisposed && !obj.Disposing)
            {
                if (obj.InvokeRequired)
                {
                    try
                    {
                        obj.Invoke(action);
                    }
                    catch (ObjectDisposedException) { }
                    catch (InvalidOperationException) { }
                    catch (InvalidAsynchronousStateException) { }
                }
                else
                {
                    action();
                }
            }
        }
    }
}