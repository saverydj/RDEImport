using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ToolsForReuse;

namespace STARS.Applications.VETS.Plugins.RDEImportTool
{
    public class TraceColumn
    {
        public string Name { get; set; }
        public string DisplayUnits { get; set; }
        public int Index { get; set; }
        public double[] Data { get; set; }
        public UnitMod Units { get; set; }

        public TraceColumn(string name, string displayUnits, int index, int dataLength, string units)
        {
            Name = name;
            DisplayUnits = displayUnits;
            Index = index;
            Data = new double[dataLength];
            Units = new UnitMod(units);
        }

    }
}
