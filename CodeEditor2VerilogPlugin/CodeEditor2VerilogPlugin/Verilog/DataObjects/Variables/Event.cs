using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.DataObjects.Variables
{
    public class Event : Variable
    {
        protected Event() { }


        public Event(string Name)
        {
            this.Name = Name;
        }

        public override Event Clone()
        {
            return Clone(Name);
        }

        public override Event Clone(string name)
        {
            Event val = new Event() { Name = name };
            return val;
        }
        public static void ParseCreateFromDeclaration(WordScanner word, NameSpace nameSpace)
        {
            //            event_declaration::= event list_of_event_identifiers ;

            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            while (!word.Eof)
            {
                if (!General.IsSimpleIdentifier(word.Text))
                {
                    word.AddError("illegal event identifier");
                    return;
                }
                Event val = new Event() { Name = word.Text };

                if (nameSpace.NamedElements.ContainsKey(val.Name))
                {
                    DataObject? dataObject = nameSpace.NamedElements.GetDataObject(val.Name);
                    if(dataObject == null)
                    {
                        word.AddError("duplicated event name");
                    }
                    else
                    {
                        Event? event_ = dataObject as Event;
                        if (event_ == null)
                        {
                            word.AddError("duplicated event name");
                        }
                        else
                        {
                            nameSpace.NamedElements.Remove(val.Name);
                            nameSpace.NamedElements.Add(val.Name, val);
                        }
                    }

                }
                else
                {
                    nameSpace.NamedElements.Add(val.Name, val);
                }


                word.Color(CodeDrawStyle.ColorType.Variable);
                word.MoveNext();

                if (word.GetCharAt(0) != ',') break;
                word.MoveNext();
            }

            if (word.Eof || word.GetCharAt(0) != ';')
            {
                word.AddError("; expected");
            }
            else
            {
                word.MoveNext();
            }

            return;
        }
    }
}
