using Avalonia.Controls;
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