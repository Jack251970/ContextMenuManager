using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace BluePointLilac.Controls
{
    sealed partial class LoadingDialog
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            progressBar = new NewProgressBar();
            panel1 = new Panel();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // progressBar
            // 
            progressBar.Dock = DockStyle.Top;
            progressBar.Location = new Point(7, 8);
            progressBar.Margin = new Padding(4);
            progressBar.Name = "progressBar";
            progressBar.Size = new Size(392, 27);
            progressBar.TabIndex = 0;
            // 
            // panel1
            // 
            panel1.AutoSize = true;
            panel1.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            panel1.BorderStyle = BorderStyle.FixedSingle;
            panel1.Controls.Add(progressBar);
            panel1.Location = new Point(0, 0);
            panel1.Margin = new Padding(0);
            panel1.MinimumSize = new Size(408, 14);
            panel1.Name = "panel1";
            panel1.Padding = new Padding(7, 8, 7, 8);
            panel1.Size = new Size(408, 45);
            panel1.TabIndex = 2;
            panel1.Resize += panel1_Resize;
            // 
            // LoadingDialog
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            BackColor = SystemColors.Control;
            ClientSize = new Size(407, 44);
            ControlBox = false;
            Controls.Add(panel1);
            FormBorderStyle = FormBorderStyle.None;
            Margin = new Padding(4, 4, 4, 4);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "LoadingDialog";
            ShowIcon = false;
            ShowInTaskbar = false;
            SizeGripStyle = SizeGripStyle.Hide;
            StartPosition = FormStartPosition.CenterParent;
            panel1.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private NewProgressBar progressBar;
        private Panel panel1;
    }

    public class NewProgressBar : ProgressBar
    {
        private Color foreColor = MyMainForm.MainColor;
        private Color backColor = MyMainForm.ButtonMain;

        public NewProgressBar()
        {
            this.SetStyle(ControlStyles.UserPaint, true);
            this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            // 不绘制背景，避免黑色出现
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            const int inset = 2; // 内边距，控制内部矩形的大小
            const int cornerRadius = 8; // 圆角半径

            // 使用高质量渲染
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            // 清除背景
            e.Graphics.Clear(this.Parent.BackColor);

            Rectangle rect = new Rectangle(0, 0, this.Width, this.Height);

            // 绘制背景（圆角矩形，使用三色渐变）
            using (GraphicsPath bgPath = CreateRoundedRectanglePath(rect, cornerRadius))
            {
                // 背景三色渐变
                Color bgColor1 = Color.FromArgb(200, 200, 200);
                Color bgColor2 = Color.FromArgb(150, 150, 150);
                Color bgColor3 = Color.FromArgb(200, 200, 200);

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