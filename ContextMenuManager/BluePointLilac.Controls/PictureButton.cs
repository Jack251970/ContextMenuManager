using System;
using System.Drawing;
using System.Windows.Forms;

namespace BluePointLilac.Controls
{
    public class PictureButton : PictureBox
    {
        public PictureButton(Image image)
        {
            BaseImage = image;
            SizeMode = PictureBoxSizeMode.AutoSize;
            Cursor = Cursors.Hand;
        }

        private Image baseImage;
        public Image BaseImage
        {
            get => baseImage;
            set
            {
                baseImage = value;
                Image = ToolStripRenderer.CreateDisabledImage(value);
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e); Image = BaseImage;
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            Image = ToolStripRenderer.CreateDisabledImage(BaseImage);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if(e.Button == MouseButtons.Left) base.OnMouseDown(e);
        }
    }
}