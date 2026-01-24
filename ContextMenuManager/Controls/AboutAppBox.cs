using BluePointLilac.Controls;
using BluePointLilac.Methods;
using ContextMenuManager.Methods;
using ContextMenuManager.Properties;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ContextMenuManager.Controls
{
    internal sealed class AboutAppBox : Panel
    {
        public AboutAppBox()
        {
            SuspendLayout();
            Dock = DockStyle.Fill;
            BackColor = DarkModeHelper.FormBack;
            Font = SystemFonts.MenuFont;
            Font = new Font(Font.FontFamily, Font.Size + 1F);
            
            // 重置所有控件属性，使用最简单可靠的设置
            
            // 设置logo图片
            pbLogo.Image = Resources.Logo;
            pbLogo.SizeMode = PictureBoxSizeMode.Zoom;
            pbLogo.Size = new Size(100, 100);
            pbLogo.Visible = true;
            
            // 设置标题标签
            lblTitle.Text = AppString.General.AppName;
            lblTitle.Font = new Font(Font.FontFamily, Font.Size + 3F, FontStyle.Bold);
            lblTitle.ForeColor = Color.Orange;
            lblTitle.TextAlign = ContentAlignment.MiddleCenter;
            lblTitle.Size = new Size(400, 30);
            lblTitle.Visible = true;
            
            // 设置描述标签 - 重写为更简单可靠的设置
            lblDescription.Text = AppString.About.Description; // 使用多语言文本
            lblDescription.TextAlign = ContentAlignment.MiddleCenter;
            lblDescription.Size = new Size(300, 30); // 调整高度为30像素
            lblDescription.Visible = true;
            lblDescription.ForeColor = DarkModeHelper.FormFore;
            lblDescription.BackColor = Color.Transparent;
            lblDescription.BorderStyle = BorderStyle.None;
            
            // 设置GitHub标签
            lblGitHub.Text = $"{AppString.About.GitHub}: https://github.com/Jack251970/ContextMenuManager"; // 使用多语言文本
            lblGitHub.TextAlign = ContentAlignment.MiddleCenter;
            lblGitHub.ForeColor = Color.Orange;
            lblGitHub.Cursor = Cursors.Hand;
            lblGitHub.Size = new Size(400, 20);
            lblGitHub.Visible = true;
            lblGitHub.MouseDown += (sender, e) => ExternalProgram.OpenWebUrl("https://github.com/Jack251970/ContextMenuManager");
            
            // 设置Gitee标签
            lblGitee.Text = $"{AppString.About.Gitee}: https://gitee.com/Jack251970/ContextMenuManager"; // 使用多语言文本
            lblGitee.TextAlign = ContentAlignment.MiddleCenter;
            lblGitee.ForeColor = Color.Orange;
            lblGitee.Cursor = Cursors.Hand;
            lblGitee.Size = new Size(400, 20);
            lblGitee.Visible = true;
            lblGitee.MouseDown += (sender, e) => ExternalProgram.OpenWebUrl("https://gitee.com/Jack251970/ContextMenuManager");
            
            // 设置许可证标签
            lblLicense.Text = $"{AppString.About.License}: GPL License"; // 使用多语言文本
            lblLicense.TextAlign = ContentAlignment.MiddleCenter;
            lblLicense.Size = new Size(400, 20);
            lblLicense.Visible = true;
            
            // 设置检查更新按钮
            btnCheckUpdate.Text = AppString.About.CheckUpdate; // 使用多语言文本
            btnCheckUpdate.Size = new Size(120, 30);
            btnCheckUpdate.BackColor = Color.Orange;
            btnCheckUpdate.ForeColor = Color.White;
            btnCheckUpdate.FlatStyle = FlatStyle.Flat;
            btnCheckUpdate.FlatAppearance.BorderSize = 0;
            btnCheckUpdate.Cursor = Cursors.Hand;
            btnCheckUpdate.Visible = true;
            btnCheckUpdate.Click += (sender, e) => Updater.Update(true);
            
            // 直接在面板上添加控件
            Controls.AddRange(new Control[] { pbLogo, lblTitle, lblDescription, lblGitHub, lblGitee, lblLicense, btnCheckUpdate });
            
            // 监听主题变化事件
            DarkModeHelper.ThemeChanged += OnThemeChanged;
            
            // 设置控件初始颜色
            UpdateControlColors();
            
            // 监听大小变化，调整控件位置和宽度（实现垂直居中）
            Resize += (sender, e) => {
                // 计算各个控件之间的间距
                const int spacingLogoTitle = 20;
                const int spacingTitleDesc = 10;
                const int spacingDescGitHub = 15;
                const int spacingGitHubGitee = 10;
                const int spacingGiteeLicense = 10;
                const int spacingLicenseBtn = 30;
                
                // 计算总高度
                int totalHeight = pbLogo.Height + spacingLogoTitle + 
                                 lblTitle.Height + spacingTitleDesc + 
                                 lblDescription.Height + spacingDescGitHub + 
                                 lblGitHub.Height + spacingGitHubGitee + 
                                 lblGitee.Height + spacingGiteeLicense + 
                                 lblLicense.Height + spacingLicenseBtn + 
                                 btnCheckUpdate.Height;
                
                // 计算起始Y坐标，实现垂直居中
                int startY = (Height - totalHeight) / 2;
                
                // 设置各个控件的位置
                pbLogo.Location = new Point((Width - pbLogo.Width) / 2, startY);
                
                lblTitle.Location = new Point(0, pbLogo.Bottom + spacingLogoTitle);
                lblTitle.Width = Width;
                
                lblDescription.Location = new Point(50, lblTitle.Bottom + spacingTitleDesc);
                lblDescription.Width = Width - 100;
                
                lblGitHub.Location = new Point(0, lblDescription.Bottom + spacingDescGitHub);
                lblGitHub.Width = Width;
                
                lblGitee.Location = new Point(0, lblGitHub.Bottom + spacingGitHubGitee);
                lblGitee.Width = Width;
                
                lblLicense.Location = new Point(0, lblGitee.Bottom + spacingGiteeLicense);
                lblLicense.Width = Width;
                
                btnCheckUpdate.Location = new Point((Width - btnCheckUpdate.Width) / 2, lblLicense.Bottom + spacingLicenseBtn);
            };
            
            // 初始布局
            OnResize(null);
            
            ResumeLayout();
        }
        
        private readonly PictureBox pbLogo = new();
        private readonly Label lblTitle = new();
        private readonly Label lblGitHub = new();
        private readonly Label lblGitee = new(); // 添加Gitee标签
        private readonly Label lblLicense = new();
        private readonly Label lblDescription = new();
        private readonly Button btnCheckUpdate = new();
        
        public void LoadAboutInfo()
        {
            // 恢复使用多语言文本
            lblTitle.Text = AppString.General.AppName;
            
            // 手动加载About类的文本，确保它们被正确初始化
            string description = AppString.About.Description;
            string gitHub = AppString.About.GitHub;
            string gitee = AppString.About.Gitee;
            string license = AppString.About.License;
            string checkUpdate = AppString.About.CheckUpdate;
            
            // 添加默认值支持，确保即使语言文件加载失败也能正常显示
            if (string.IsNullOrEmpty(description)) description = "一个纯粹的Windows右键菜单管理器";
            if (string.IsNullOrEmpty(gitHub)) gitHub = "GitHub";
            if (string.IsNullOrEmpty(gitee)) gitee = "Gitee";
            if (string.IsNullOrEmpty(license)) license = "许可证";
            if (string.IsNullOrEmpty(checkUpdate)) checkUpdate = "检查更新";
            
            // 设置控件文本
            lblDescription.Text = description;
            lblGitHub.Text = $"{gitHub}: https://github.com/Jack251970/ContextMenuManager";
            lblGitee.Text = $"{gitee}: https://gitee.com/Jack251970/ContextMenuManager";
            lblLicense.Text = $"{license}: GPL License";
            btnCheckUpdate.Text = checkUpdate;
            
            // 确保控件可见
            Visible = true;
        }
        
        // 主题变化事件处理程序
        private void OnThemeChanged(object sender, EventArgs e)
        {
            UpdateControlColors();
        }
        
        // 更新控件颜色以适应主题变化
        private void UpdateControlColors()
        {
            // 更新面板背景色
            BackColor = DarkModeHelper.FormBack;
            
            // 更新标签文字颜色
            lblTitle.ForeColor = Color.Orange; // 保持橘色
            lblDescription.ForeColor = DarkModeHelper.FormFore;
            lblGitHub.ForeColor = Color.Orange; // 保持橘色
            lblGitee.ForeColor = Color.Orange; // 保持橘色
            lblLicense.ForeColor = DarkModeHelper.FormFore;
            
            // 更新按钮颜色
            btnCheckUpdate.BackColor = Color.Orange; // 保持橘色
            btnCheckUpdate.ForeColor = Color.White; // 保持白色文字
        }
        
        // 重写Dispose方法，取消订阅事件
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DarkModeHelper.ThemeChanged -= OnThemeChanged;
            }
            base.Dispose(disposing);
        }
    }
}