using System;
using System.Collections.Generic;
using System.Text;

namespace DigitaltKladdpapperReadingPDFs
{
    public class Paragraph : IContent
    {
        public string Text { get; set; }
        public StyleInfo StyleInfo { get; set; }
    }
}
