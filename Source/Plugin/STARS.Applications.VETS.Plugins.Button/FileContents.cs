using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace STARS.Applications.VETS.Plugins.RDEImportTool
{
    public class FileContents
    {
        public string[] Lines { get; private set; }
        public char Delimiter { get; private set; }
        public int NamesIndex { get; private set; }
        public int UnitsIndex { get; private set; }
        public int DataIndex { get; private set; }
        public int DataLength { get; private set; }
        public string[] NamesSplit { get; private set; }
        public string[] UnitsSplit { get; private set; }


        public FileContents(string[] lines)
        {
            Lines = lines;
            Delimiter = Convert.ToChar(lines[0].Split(new string[] { "QuantityDelimiter = " }, StringSplitOptions.None)[1]);

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("time") && lines[i].Contains("speed"))
                {
                    NamesIndex = i;
                    UnitsIndex = i + 1;
                    DataIndex = i + 2;
                    DataLength = lines.Length - DataIndex;
                    break;
                }
            }

            NamesSplit = Lines[NamesIndex].Split(Delimiter);
            UnitsSplit = Lines[UnitsIndex].Split(Delimiter);
        }
        
    }
}
