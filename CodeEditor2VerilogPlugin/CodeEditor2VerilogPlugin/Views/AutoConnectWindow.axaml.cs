using AjkAvaloniaLibs.Controls;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using CodeEditor2.CodeEditor.PopupMenu;
using CodeEditor2.Data;
using DynamicData;
using ExCSS;
using pluginVerilog.Data;
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
using static pluginVerilog.CodeDrawStyle;
using static pluginVerilog.Verilog.ModPort;

namespace pluginVerilog.Views
{
    public partial class AutoConnectWindow : Window
    {
        public AutoConnectWindow()
        {
            InitializeComponent();
            if (Design.IsDesignMode) return;
            throw new NotImplementedException();
        }

        private TextBlock HeaderTextBlock = new TextBlock();
        private TextBlock BottomTextBlock = new TextBlock();
        private ListBox ListBox0 = new ListBox();
        public AutoConnectWindow(ModuleInstantiation moduleInstantiation, BuildingBlock buildingBlock)
        {
            InitializeComponent();

            CodeEditor2.Tools.VerticalGridConstructor gridConstructor = new CodeEditor2.Tools.VerticalGridConstructor();
            Content = gridConstructor.Grid;

            gridConstructor.AppendContol(HeaderTextBlock,(int)FontSize);

            gridConstructor.AppendContolFill(ListBox0);

            gridConstructor.AppendContol(BottomTextBlock, (int)FontSize);

            FontSize = CodeEditor2.Controller.CodeEditor.FontSize;

            this.moduleInstantiation = moduleInstantiation;
            HeaderTextBlock.Text = moduleInstantiation.SourceName + " " + moduleInstantiation.Name;
            HeaderTextBlock.FontSize = FontSize;
            HeaderTextBlock.Text = "endmodule";
            BottomTextBlock.FontSize = FontSize;

            BuildingBlock? instanedBuildingBlock = moduleInstantiation.GetInstancedBuildingBlock();
            IPortNameSpace? portNameSpace = instanedBuildingBlock as IPortNameSpace;
            if (portNameSpace == null || instanedBuildingBlock == null)
            {
                return;
            }

            string? portGroup = null;

            foreach (Verilog.DataObjects.Port port in portNameSpace.PortsList)
            {
                if (port.PortGroupName != portGroup && portGroup != "")
                {
                    portGroup = port.PortGroupName;
                    if (portGroup != null)
                    {
                        CommentItem commentItem = new CommentItem("// " + portGroup);
                        commentItem.FontSize = FontSize;
                        ListBox0.Items.Add(commentItem);
                    }
                }
                ConnectionItem item;
                if (!moduleInstantiation.PortConnection.ContainsKey(port.Name))
                {
                    item = new ConnectionItem(port, null,instanedBuildingBlock,buildingBlock);
                }
                else
                {
                    item = new ConnectionItem(port, moduleInstantiation.PortConnection[port.Name],instanedBuildingBlock, buildingBlock);
                }
                item.FontSize = FontSize;
                ListBox0.Items.Add(item);
            }
            Ready = true;

            ListBox0.AddHandler(KeyDownEvent, ListBox0_KeyDown,
               RoutingStrategies.Tunnel | RoutingStrategies.Bubble,
               handledEventsToo: true);

            if(ListBox0.Items.Count > 0)
            {
                ListBox0.SelectedIndex = 0;
            }

            Loaded += AutoConnectWindow_Loaded;
        }

        private void AutoConnectWindow_Loaded(object? sender, RoutedEventArgs e)
        {
            Position = new PixelPoint((int)(Position.X + Width * 0.1), (int)(Position.Y + Height * 0.1));
            Width = Width * 0.8;
            Height = Height * 0.8;
        }

        ModuleInstantiation? moduleInstantiation;
        public bool Ready = false;
        public bool Accept = false;

        public void Complete()
        {
            if (moduleInstantiation == null) return;
            Accept = true;


            foreach(var item in ListBox0.Items)
            {
                ConnectionItem? citem = item as ConnectionItem;
                if (citem == null) continue;

                if (moduleInstantiation.PortConnection.ContainsKey(citem.Port.Name))
                {
                    moduleInstantiation.PortConnection[citem.Port.Name] = Verilog.Expressions.Expression.CreateTempExpression(citem.target.CreateString());
                }
                else
                {
                    moduleInstantiation.PortConnection.Add(citem.Port.Name,Verilog.Expressions.Expression.CreateTempExpression(citem.target.CreateString()));
                }
            }
            Close();
        }

        public void Cancel()
        {
            Close();
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
            else if (e.Key == Avalonia.Input.Key.Right)
            {
                connection.Reject();
            }
            else if (e.Key == Avalonia.Input.Key.Enter)
            {
                Complete();
            }
            else if (e.Key == Avalonia.Input.Key.Escape)
            {
                Cancel();
            }
        }


        List<(string, string?)> ConnectionList = new List<(string, string?)>();

        public class CommentItem : AjkAvaloniaLibs.Controls.ListViewItem
        {
            public CommentItem(string comment) : base()
            {
                this.comment = comment;
                UpdateVisual();
            }
            string comment;
            public void UpdateVisual()
            {
                ColorLabel.AppendText(comment, Global.CodeDrawStyle.Color(ColorType.Comment));
                TextBlock.Inlines?.Clear();
                ColorLabel.AppendToTextBlock(TextBlock);
            }
            public AjkAvaloniaLibs.Controls.ColorLabel ColorLabel = new AjkAvaloniaLibs.Controls.ColorLabel();
        }
        public class ConnectionItem : AjkAvaloniaLibs.Controls.ListViewItem
        {
            public ConnectionItem(Verilog.DataObjects.Port port, Verilog.Expressions.Expression? expression,BuildingBlock instanceBuildingBlock,BuildingBlock buildingBlock) : base()
            {
                this.Port = port;
                this.Expression = expression;
                this.InstanceBuildingBlock = instanceBuildingBlock;
                this.BuildingBlock = buildingBlock;
                if (expression != null)
                {
                    this.original = expression.GetLabel();
                    this.target = original;
                }

                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
                Background = new SolidColorBrush(Avalonia.Media.Colors.Transparent);
                cantidates = GetCantidates();

                UpdateVisual();
            }

            public pluginVerilog.Verilog.DataObjects.Port Port;
            Verilog.Expressions.Expression? Expression;
            BuildingBlock InstanceBuildingBlock;
            BuildingBlock BuildingBlock;
            List<ColorLabel> cantidates;
            public ColorLabel target = new ColorLabel();
            ColorLabel original = new ColorLabel();

            public AjkAvaloniaLibs.Controls.ColorLabel ColorLabel = new AjkAvaloniaLibs.Controls.ColorLabel();
            public void UpdateVisual()
            {
                ColorLabel.Clear();
                ColorLabel.AppendText(".");
                ColorLabel.AppendText(Port.Name, Global.CodeDrawStyle.Color(ColorType.Identifier));
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

            private List<string> removePortSuffixs = new List<string>() { "_I", "_O", "_IO" };

            private List<ColorLabel> GetCantidates()
            {
                List<(int, ColorLabel)> cantidates = new List<(int, ColorLabel)>();
                
                foreach(var namedElement in BuildingBlock.NamedElements)
                {
                    string portName = Port.Name;

                    if (namedElement.Name.ToLower() == portName.ToLower()) {
                        ColorLabel label = new ColorLabel();
                        label.AppendText(namedElement.Name, Avalonia.Media.Colors.Red);
                        cantidates.Add((0, label));
                        continue;
                    }

                    foreach(string removePortSuffix in removePortSuffixs)
                    {
                        if (Port.Name.EndsWith(removePortSuffix))
                        {
                            portName = Port.Name.Substring(0,Port.Name.Length - removePortSuffix.Length);

                            if (namedElement.Name.ToLower() == portName.ToLower())
                            {
                                ColorLabel label = new ColorLabel();
                                label.AppendText(namedElement.Name, Avalonia.Media.Colors.Red);
                                cantidates.Add((0, label));
                                continue;
                            }
                        }
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
                target = new ColorLabel();
                UpdateVisual();
            }
        }
    }
}
