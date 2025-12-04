using AjkAvaloniaLibs.Controls;
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
using System.Linq;
using System.Linq.Expressions;
using Tmds.DBus.Protocol;
using static pluginVerilog.Verilog.ModPort;

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
            if (portNameSpace == null || buildingBlock == null)
            {
                return;
            }

            foreach (Verilog.DataObjects.Port port in portNameSpace.PortsList)
            {
                ConnectionItem item;
                if (!moduleInstantiation.PortConnection.ContainsKey(port.Name))
                {
                    item = new ConnectionItem(port, null,buildingBlock);
                }
                else
                {
                    item = new ConnectionItem(port, moduleInstantiation.PortConnection[port.Name],buildingBlock);
                }
                ListBox0.Items.Add(item);
            }
            Ready = true;

            ListBox0.AddHandler(KeyDownEvent, ListBox0_KeyDown,
               RoutingStrategies.Tunnel | RoutingStrategies.Bubble,
               handledEventsToo: true);
//            ListBox0.KeyDown += ListBox0_KeyDown;
        }

        private void ListBox0_KeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            object? item = ListBox0.SelectedItem;
            ConnectionItem? connection = item as ConnectionItem;
            if (connection == null) return;

            if(e.Key == Avalonia.Input.Key.Left)
            {
                connection.Accept();
            }
            else if(e.Key == Avalonia.Input.Key.Right)
            {
                connection.Reject();
            }
        }

        public bool Ready = false;
        public bool Accept = false;

        List<(string, string?)> connectionList = new List<(string, string?)>();
        public class ConnectionItem : AjkAvaloniaLibs.Controls.ListViewItem
        {
            public ConnectionItem(Verilog.DataObjects.Port port, Verilog.Expressions.Expression? expression,BuildingBlock buildingBlock) : base()
            {
                this.Port = port;
                this.Expression = expression;
                this.BuildingBlock = buildingBlock;

                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
                Background = new SolidColorBrush(Avalonia.Media.Colors.Transparent);
                if (Expression != null) target.AppendText(Expression.CreateString());
                cantidates = GetCantidates();

                UpdateVisual();
            }
            pluginVerilog.Verilog.DataObjects.Port Port;
            Verilog.Expressions.Expression? Expression;
            BuildingBlock BuildingBlock;
            List<ColorLabel> cantidates;
            ColorLabel target = new ColorLabel();

            public AjkAvaloniaLibs.Controls.ColorLabel ColorLabel = new AjkAvaloniaLibs.Controls.ColorLabel();
            public void UpdateVisual()
            {
                ColorLabel.Clear();
                ColorLabel.AppendText(".");
                ColorLabel.AppendText(Port.Name);
                ColorLabel.AppendText("(");
                ColorLabel.AppendLabel(target);
                ColorLabel.AppendText(")");
                ColorLabel.AppendToTextBlock(TextBlock);
                if (cantidates.Count != 0 && cantidates[0] != target)
                {
                    ColorLabel.AppendText(" -> ");
                    ColorLabel.AppendLabel(cantidates[0]);
                }
                TextBlock.Inlines?.Clear();
                ColorLabel.AppendToTextBlock(TextBlock);
            }

            private List<ColorLabel> GetCantidates()
            {
                List<(int, ColorLabel)> cantidates = new List<(int, ColorLabel)>();
                
                foreach(var namedElement in BuildingBlock.NamedElements)
                {
                    if (namedElement.Name.ToLower() == Port.Name.ToLower()) {
                        ColorLabel label = new ColorLabel();
                        label.AppendText(namedElement.Name, Avalonia.Media.Colors.Red);
                        cantidates.Add((0, label));
                    }
                }

                List<ColorLabel> colorLabels = new List<ColorLabel>();
                colorLabels = cantidates
                    .OrderBy(c => c.Item1)
                    .Select(c => c.Item2)
                    .ToList();
                return colorLabels;
            }


            public void Accept()
            {
                if (cantidates.Count != 0)
                {
                    target = cantidates[0];
                }
                UpdateVisual();
            }

            public void Reject()
            {
                target.Clear();
                if (Expression != null)
                {
                    target.AppendText(Expression.CreateString()); 
                }
                UpdateVisual();
            }
        }
    }
}
