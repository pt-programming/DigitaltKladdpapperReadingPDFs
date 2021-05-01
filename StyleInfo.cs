using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Graphics.Colors;
using UglyToad.PdfPig.PdfFonts;

namespace DigitaltKladdpapperReadingPDFs
{
    public class StyleInfo
    {
        public IColor Color { get; set; }
        public FontDetails Font { get; set; }
        public string FontName { get; set; }
        public double FontSize { get; set; }
        public TextOrientation TextOrientation { get; set; }

        public float StartBaseLine { get; set; }
        public float EndBaseLine { get; set; }
        public float[] GlyphRectangle { get; set; } //
        public float Location { get; set; }
        public float PointSize { get; set; }
        public float Width { get; set; }

        public bool Empty() => Color == null && Font == null && FontName == null && FontSize.Equals(0) && TextOrientation.Equals(UglyToad.PdfPig.Content.TextOrientation.Other);

        protected bool Equals(StyleInfo other)
        {
            return Color.Equals(other.Color) && Font.Equals(other.Font) && FontName == other.FontName && FontSize.Equals(other.FontSize) && TextOrientation == other.TextOrientation;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((StyleInfo)obj);
        }

        protected bool SameRow(StyleInfo other)
        {
            return TextOrientation == other.TextOrientation && StartBaseLine.Equals(other.StartBaseLine) && EndBaseLine.Equals(other.EndBaseLine);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Color, Font, FontName, FontSize, TextOrientation);
        }
    }
}
