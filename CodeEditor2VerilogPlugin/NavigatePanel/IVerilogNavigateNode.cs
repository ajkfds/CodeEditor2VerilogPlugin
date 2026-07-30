using System.Threading.Tasks;

namespace pluginVerilog.NavigatePanel
{
    public interface IVerilogNavigateNode
    {
        Data.IVerilogRelatedFile? VerilogRelatedFile { get; }

        string Text { get; }

        //void DrawNode(Graphics graphics, int x, int y, Font font, Color color, Color backgroundColor, Color selectedColor, int lineHeight, bool selected);

        void OnSelected();

        Task UpdateAsync();

    }
}
