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

            CodeEditor2.Tools.VerticalGridConstructor gridConstructor = new VerticalGridConstructor();
            tab.Content = gridConstructor.Grid;

            gridConstructor.AppendText("Parse Option",true);
            gridConstructor.AppendText("option");

            compileOptionText = new TextBox() { 
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap
            };
            gridConstructor.AppendContolFill(compileOptionText);

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
