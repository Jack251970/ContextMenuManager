using Microsoft.AspNetCore.Http.HttpResults;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace BluePointLilac.Controls
{
    sealed partial class LoadingDialog
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
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
            progressBar.Location = new System.Drawing.Point(10, 12);
            progressBar.Margin = new Padding(5, 6, 5, 6);
            progressBar.Name = "progressBar";
            progressBar.Size = new System.Drawing.Size(560, 40);
            progressBar.Style = ProgressBarStyle.Blocks;
            progressBar.TabIndex = 0;
            progressBar.Value = 0;
            progressBar.Maximum = 100;
            progressBar.Minimum = 0;
            // 
            // panel1
            // 
            panel1.AutoSize = true;
            panel1.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            panel1.BorderStyle = BorderStyle.FixedSingle;
            panel1.Controls.Add(progressBar);
            panel1.Location = new System.Drawing.Point(0, 0);
            panel1.Margin = new Padding(0);
            panel1.MinimumSize = new System.Drawing.Size(582, 19);
            panel1.Name = "panel1";
            panel1.Padding = new Padding(10, 12, 10, 12);
            panel1.Size = new System.Drawing.Size(582, 66);
            panel1.TabIndex = 2;
            panel1.Resize += panel1_Resize;
            // 
            // LoadingDialog
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            BackColor = System.Drawing.SystemColors.Control;
            ClientSize = new System.Drawing.Size(581, 64);
            ControlBox = false;
            Controls.Add(panel1);
            FormBorderStyle = FormBorderStyle.None;
            Margin = new Padding(5, 6, 5, 6);
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
        private Panel panel1;
        private NewProgressBar progressBar;
    }

    public class NewProgressBar : ProgressBar
    {
        private Color foreColor = MyMainForm.MainColor;
        private Color backColor = MyMainForm.ButtonMain;

        public NewProgressBar()
        {
            this.SetStyle(ControlStyles.UserPaint, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Rectangle rec = e.ClipRectangle;

            rec.Width = (int)(rec.Width * ((double)Value / Maximum)) - 4;
            if (ProgressBarRenderer.IsSupported)
                ProgressBarRenderer.DrawHorizontalBar(e.Graphics, e.ClipRectangle);
            rec.Height = rec.Height - 4;
            using (Brush backBrush = new SolidBrush(backColor))
                e.Graphics.FillRectangle(backBrush, 2, 2, e.ClipRectangle.Width - 4, rec.Height);
            using (Brush foreBrush = new SolidBrush(foreColor))
                e.Graphics.FillRectangle(foreBrush, 2, 2, rec.Width, rec.Height);
        }
    }
}