using BluePointLilac.Methods;
using ContextMenuManager.Methods;

namespace ContextMenuManager.Controls.Interfaces
{
    internal interface ITsiWebSearchItem
    {
        string SearchText { get; }
        WebSearchMenuItem TsiSearch { get; set; }
    }

    internal sealed class WebSearchMenuItem : RToolStripMenuItem
    {
        public WebSearchMenuItem(ITsiWebSearchItem item) : base(AppString.Menu.WebSearch)
        {
            Click += (sender, e) =>
            {
                var url = AppConfig.EngineUrl.Replace("%s", item.SearchText);
                ExternalProgram.OpenWebUrl(url);
            };
        }
    }
}