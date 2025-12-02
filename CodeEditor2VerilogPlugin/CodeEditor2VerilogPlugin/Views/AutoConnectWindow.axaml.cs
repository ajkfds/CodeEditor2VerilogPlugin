using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using CodeEditor2.CodeEditor.PopupMenu;
using CodeEditor2.Data;
using DynamicData;
using pluginVerilog.Verilog;
using pluginVerilog.Verilog.BuildingBlocks;
using pluginVerilog.Verilog.DataObjects;
using pluginVerilog.Verilog.ModuleItems;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;

namespace pluginVerilog.Views
{
    public partial class AutoConnectWindow : Window
    {
        public AutoConnectWindow(ModuleInstantiation moduleInstantiation)
        {
            InitializeComponent();

            HeaderTextBlock.Text = moduleInstantiation.SourceName + " " + moduleInstantiation.Name;

            BuildingBlock? buildingBlock = moduleInstantiation.GetInstancedBuildingBlock();
            IPortNameSpace? portNameSpace = buildingBlock as IPortNameSpace;
            if (portNameSpace == null)
            {
                Close();
                return;
            }

            foreach (Port port in portNameSpace.PortsList)
            {
                (string, string?) connection;
                if (!moduleInstantiation.PortConnection.ContainsKey(port.Name))
                {
                    connection = (port.Name, null);
                }
                else
                {
                    connection = (port.Name, moduleInstantiation.PortConnection[port.Name].CreateString());
                }
                connectionList.Add(connection);
                ConnectionItem item = new ConnectionItem(connection);
                ListBox0.Items.Add(item);
            }


            //Style style = new Style();
            //style.Selector = ((Selector?)null).OfType(typeof(ListBoxItem));
            //style.Add(new Setter(Layoutable.MinHeightProperty, 8.0));
            //style.Add(new Setter(Layoutable.HeightProperty, 14.0));
            //ListBox0.Styles.Add(style);

            //            KeyDown += PopupMenuView_KeyDown;
            //            LostFocus += PopupMenuView_LostFocus;
            //            TextBox0.TextChanged += TextBox0_TextChanged;

            //if (ListBox0.Items.Count > 0)
            //{
            //    ListBox0.SelectedIndex = 0;
            //}
        }

        List<(string, string?)> connectionList = new List<(string, string?)>();
        public class ConnectionItem : AjkAvaloniaLibs.Controls.ListViewItem
        {
            public ConnectionItem((string,string?) connection) : base()
            {
//                Content = StackPanel;
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
                Background = new SolidColorBrush(Avalonia.Media.Colors.Transparent);

                RenderOptions.SetBitmapInterpolationMode(IconImage, Avalonia.Media.Imaging.BitmapInterpolationMode.HighQuality);

                //StackPanel.Children.Add(ColorLabel);

                ColorLabel.AppendText(".");
                ColorLabel.AppendText(connection.Item1);
                ColorLabel.AppendText("(");
                if(connection.Item2 != null) ColorLabel.AppendText(connection.Item2);
                ColorLabel.AppendText(")");
                ColorLabel.AppendToTextBlock(TextBlock);
                //Text = text;
                //Height = 14;
                //FontSize = 8;
                //Padding = new Avalonia.Thickness(0, 0, 2, 2);
                //Margin = new Avalonia.Thickness(0, 0, 0, 0);
            }
            public AjkAvaloniaLibs.Controls.ColorLabel ColorLabel = new AjkAvaloniaLibs.Controls.ColorLabel();


            public Action? Selected;
            public virtual void OnSelected()
            {
                if (Selected != null) Selected();
            }
        }
    }
}
