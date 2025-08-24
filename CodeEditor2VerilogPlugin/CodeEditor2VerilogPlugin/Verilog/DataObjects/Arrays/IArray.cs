using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.DataObjects.Arrays
{
    public interface IArray
    {
        public int? Size { get; }
        public bool Constant { get; }

        public virtual string CreateString()
        {
            throw new NotImplementedException();
        }

        public virtual void AppendLabel(AjkAvaloniaLibs.Controls.ColorLabel label)
        {
            throw new NotImplementedException();
        }
        public virtual AjkAvaloniaLibs.Controls.ColorLabel GetLabel()
        {
            throw new NotImplementedException();
        }
        public virtual bool CheckIndexRangeError(Expressions.Expression indexExpression)
        {
            throw new NotImplementedException();
        }
    }
}
