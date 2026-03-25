using ContextMenuManager.Methods;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ContextMenuManager.Controls
{
    public class MyList : UserControl
    {
        protected readonly StackPanel stackPanel = new();
        protected readonly ScrollViewer scrollViewer = new();

        public MyList()
        {
            scrollViewer.Content = stackPanel;
            scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            Content = scrollViewer;
        }

        public UIElementCollection Controls => stackPanel.Children;

        public bool Visible
        {
            get => Visibility == Visibility.Visible;
            set => Visibility = value ? Visibility.Visible : Visibility.Collapsed;
        }

        private MyListItem hoveredItem;
        public MyListItem HoveredItem
        {
            get => hoveredItem;
            set
            {
                if (hoveredItem == value) return;
                hoveredItem = value;
                HoveredItemChanged?.Invoke(this, null);
            }
        }

        public event EventHandler HoveredItemChanged;

        public void AddItem(MyListItem item)
        {
            stackPanel.Children.Add(item.Control);
        }

        public void AddItems(MyListItem[] items)
        {
            Array.ForEach(items, AddItem);
        }

        public void AddItems(List<MyListItem> items)
        {
            items.ForEach(AddItem);
        }

        public void SetItemIndex(MyListItem item, int newIndex)
        {
            stackPanel.Children.Remove(item.Control);
            stackPanel.Children.Insert(newIndex, item.Control);
        }

        public int GetItemIndex(MyListItem item)
        {
            return stackPanel.Children.IndexOf(item.Control);
        }

        public void InsertItem(MyListItem item, int index)
        {
            if (item == null) return;
            stackPanel.Children.Insert(index, item.Control);
        }

        public virtual void ClearItems()
        {
            stackPanel.Children.Clear();
        }

        public void SortItemByText()
        {
            var items = stackPanel.Children.OfType<MyListItem>().ToList();
            stackPanel.Children.Clear();
            items.Sort(new TextComparer());
            items.ForEach(AddItem);
        }

        public class TextComparer : IComparer<MyListItem>
        {
            public int Compare(MyListItem x, MyListItem y)
            {
                return string.Compare(x.Text, y.Text, StringComparison.OrdinalIgnoreCase);
            }
        }

        public void Dispose()
        {
            Content = null;
        }
    }

    public class MyListItem
    {
        public MyUserControl Control { get; protected set; }

        protected readonly Grid grid;
        protected readonly Image imgIcon;
        protected readonly TextBlock txtTitle;
        protected readonly StackPanel flpControls;

        protected MyList List;

        public MyListItem(MyList list)
        {
            if (list != null)
            {
                List = list;
                Control = new MyUserControl()
                {
                    Item = this
                };

                grid = new();
                imgIcon = new() { Width = 32, Height = 32, Margin = new Thickness(20, 0, 10, 0) };
                txtTitle = new() { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 0, 0) };
                flpControls = new() { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 0, 20, 0) };

                Control.Height = 50;
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                Grid.SetColumn(imgIcon, 0);
                Grid.SetColumn(txtTitle, 1);
                Grid.SetColumn(flpControls, 2);

                grid.Children.Add(imgIcon);
                grid.Children.Add(txtTitle);
                grid.Children.Add(flpControls);

                Control.Content = grid;

                Control.MouseEnter += (s, e) => Control.Background = (SolidColorBrush)Application.Current.Resources["ListViewItemBackgroundPointerOver"];
                Control.MouseLeave += (s, e) => Control.Background = Brushes.Transparent;
            }
        }

        private System.Drawing.Image _image;
        public System.Drawing.Image Image
        {
            get => _image;
            set
            {
                _image = value;
                if (value != null) imgIcon.Source = value.ToBitmapSource();
                else imgIcon.Source = null;
            }
        }

        private string _text;
        public string Text
        {
            get => _text;
            set
            {
                _text = value;
                if (List != null) txtTitle.Text = value;
            }
        }

        private bool _visible;
        public bool Visible
        {
            get => _visible;
            set
            {
                _visible = value;
                if (List != null) Control.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public System.Drawing.Font Font { get; set; }

        public string SubText { get; set; }

        public bool HasImage
        {
            get => imgIcon.Visibility == Visibility.Visible;
            set => imgIcon.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
        }

        public void Dispose()
        {
            if (List != null) Control.Content = null;
        }

        public virtual void Indent()
        {

        }

        public void AddCtr(UIElement ctr)
        {
            AddCtr(ctr, 10);
        }

        public void AddCtr(UIElement ctr, int space)
        {
            if (ctr is FrameworkElement fe)
            {
                fe.VerticalAlignment = VerticalAlignment.Center;
                fe.Margin = new Thickness(0, 0, space, 0);
            }
            flpControls.Children.Insert(0, ctr);
        }

        public void AddCtrs(UIElement[] ctrs)
        {
            Array.ForEach(ctrs, AddCtr);
        }

        public void SetCtrIndex(UIElement ctr, int newIndex)
        {
            if (flpControls.Children.Contains(ctr))
            {
                flpControls.Children.Remove(ctr);
                flpControls.Children.Insert(newIndex, ctr);
            }
        }
    }

    public class MyUserControl : UserControl
    {
        public MyListItem Item { get; set; }
    }
}
