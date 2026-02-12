using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.DataObjects.DataTypes
{
    public interface IDataType
    {
        public DataTypeEnum Type { get; }
        public string CreateString();

        public bool Packable { get; }

        public bool PartSelectable { get; }

        public void AppendTypeLabel(AjkAvaloniaLibs.Controls.ColorLabel label);


        public int? BitWidth { get; }
        public CodeDrawStyle.ColorType ColorType { get; }
        public bool IsVector { get; }

        public IDataType Clone();
        public List<Arrays.PackedArray> PackedDimensions { get; }

        /*
        In SystemVerilog, the types that can be specified as the data type for a built-in net (such as **`wire`**) are restricted to ensure the net can correctly model hardware behavior, including signal strengths and 4-state logic.

        The following is a summary of valid data types for built-in nets:

        ### 1. Valid Data Types for Built-in Nets (`wire`, `tri`, etc.)
        A valid data type for a built-in net must meet the following criteria:
        *   **4-state Integral Types**: Any 4-state data type can be used to declare a net. This includes **`logic`**, **`reg`**, **`integer`**, and **`time`**.
        *   **Packed Aggregates**: **Packed arrays** and **packed structures** are allowed. If any member of a packed structure or union is a 4-state type, the entire structure is treated as 4-state.
        *   **Fixed-size Unpacked Aggregates**: **Fixed-size unpacked arrays** and **unpacked structures** are permitted, provided that **every element** within them also has a valid data type for a net.

        ### 2. Key Rules and Behaviors
        *   **4-state Composition**: The recursive definition of valid types ensures that a built-in net is composed entirely of **4-state bits** (0, 1, x, z). This allows each bit to carry additional **strength information**, which is used to resolve conflicts between multiple drivers.
        *   **Default Data Type**: If a net is declared without an explicit data type (e.g., `wire w;`), it implicitly defaults to the **`logic`** data type.
        *   **Initialization**: Built-in nets (except `trireg`) default to an initial value of **`z`**. If they have no drivers, they remain at `z`.

        ### 3. Prohibited Types
        The following types **cannot** be used as the data type for a built-in net:
        *   **2-state Types**: Types such as `bit`, `int`, `byte`, `shortint`, and `longint` are 2-state and do not support the `x` and `z` states required for standard net resolution.
        *   **Dynamic Types**: **Class handles**, **dynamic arrays**, **associative arrays**, and **queues** cannot be used for built-in nets because they are dynamic objects that only exist as variables.
        *   **Real Types**: The **`real`** and **`shortreal`** types are not permitted for standard built-in nets.

        ### 4. Special Case: User-defined Nettypes
        If you use the **`nettype`** keyword to create a **user-defined nettype**, the restrictions are relaxed. A user-defined nettype can be:
        *   A 2-state or 4-state integral type.
        *   A **`real`** or **`shortreal`** type.
        *   A fixed-size unpacked aggregate where each element is valid for a user-defined nettype.

        However, standard nets like `wire` are strictly limited to the 4-state bit models described in Section 1. 
         */
        public bool IsValidForNet { get; }

    }
}
