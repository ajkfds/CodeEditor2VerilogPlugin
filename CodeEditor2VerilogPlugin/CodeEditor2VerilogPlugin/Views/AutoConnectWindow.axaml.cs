using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Styling;
using CodeEditor2.CodeEditor.PopupMenu;
using CodeEditor2.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;

namespace pluginVerilog.Views
{
    public partial class AutoConnectWindow : Window
    {
        public AutoConnectWindow()
        {
            InitializeComponent();

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
    }
}
