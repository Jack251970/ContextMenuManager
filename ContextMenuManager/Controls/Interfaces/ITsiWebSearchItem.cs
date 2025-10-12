using BluePointLilac.Methods;
using ContextMenuManager.Methods;

namespace ContextMenuManager.Controls.Interfaces
{
    interface ITsiWebSearchItem
    {
        string SearchText { get; }
        WebSearchMenuItem TsiSearch { get; set; }
    }

    sealed class WebSearchMenuItem : RToolStripMenuItem
    {
        public WebSearchMenuItem(ITsiWebSearchItem item) : base(AppString.Menu.WebSearch)
        {
            Click += (sender, e) =>
            {
                string url = AppConfig.EngineUrl.Replace("%s", item.SearchText);
                ExternalProgram.OpenWebUrl(url);
            };
        }
    }
}