using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System.Collections.ObjectModel;

namespace pluginVerilog.Tool;

public partial class InstantiateSelect : UserControl
{
    private ObservableCollection<ListBoxItem> listItems = new ObservableCollection<ListBoxItem>();
    public InstantiateSelect()
    {
        InitializeComponent();
        ListBox0.ItemsSource = listItems;
    }
}