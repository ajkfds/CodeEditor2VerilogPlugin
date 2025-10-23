using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using CodeEditor2.Data;
using CodeEditor2.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Views
{
    public class ProjectPropertyTab
    {

        TabItem tab;
        Project project;
        ProjectProperty projectProperty;
        ItemPropertyForm form;
        CodeEditor2.NavigatePanel.NavigatePanelNode node;
        TextBox compileOptionText = new TextBox();
        public ProjectPropertyTab(ProjectProperty projectProperty, ItemPropertyForm form, CodeEditor2.NavigatePanel.NavigatePanelNode node,Project project)
        {
            this.projectProperty = projectProperty;
            this.project = project;
            this.form = form;
            this.node = node;

            tab = new TabItem() { Name = "verilog", Header = "Verilog", FontSize = 14 };
            form.TabControl.Items.Add(tab);

            Grid grid = new Grid() { Margin = new Avalonia.Thickness(4) };
            tab.Content = grid;

            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Star));

            TextBlock text0 = new TextBlock();
            {
                Bold bold = new Bold();
                bold.Inlines.Add(new Run("Compile Option"));
                text0.Inlines?.Add(bold);
            }
            Grid.SetRow(text0, 0);
            grid.Children.Add(text0);

            TextBlock text1 = new TextBlock() { Text = "%IncludeFiles% : list of include files" };
            Grid.SetRow(text1, 1);
            grid.Children.Add(text1);

            TextBlock text2 = new TextBlock() { Text = "%Files% : list of files" };
            Grid.SetRow(text2, 2);
            grid.Children.Add(text2);

            compileOptionText = new TextBox() { 
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetRow(compileOptionText, 3);
            grid.Children.Add(compileOptionText);

            form.OkButtonControl.Click += OkButtonControl_Click;

            compileOptionText.Text = projectProperty.CompileOption;
        }
        private void OkButtonControl_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (this.compileOptionText.Text == null)
            {
                projectProperty.CompileOption = "";
            }
            else
            {
                projectProperty.CompileOption = compileOptionText.Text;
            }
        }
    }
}
