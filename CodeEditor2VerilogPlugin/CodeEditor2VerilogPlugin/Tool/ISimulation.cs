using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace pluginVerilog.Tool
{
    public interface ISimulation
    {
        public Data.VerilogFile TopFile { get; set; }
        public Task<string> RunSimulationAsync(CancellationToken cancellationToken);

        
    }
}
