using System;
using System.Collections.Generic;
using System.Text;

namespace DigitaltKladdpapperReadingPDFs
{
    public class Table : IContent
    {
        private int NumberOfRows { get; set; }
        private int NumberOfColumns { get; set; }
        private List<TableRow> TableRows { get; set; }
    }
}
