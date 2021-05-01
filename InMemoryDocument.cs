using System;
using System.Collections.Generic;
using System.Text;

namespace DigitaltKladdpapperReadingPDFs
{
    public struct InMemoryPdfDocument
    {
        public string Header { get; set; }
        public string Footer { get; set; }
        public string Title { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }

        public List<IContent> Content { get; set; }
    }
}
