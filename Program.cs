using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.Export.Alto;
using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using UglyToad.PdfPig.Graphics.Colors;

namespace DigitaltKladdpapperReadingPDFs
{
    public class Program
    {
        public static void Main()
        {
            //string path = "../../../Examplefiles/pdf/1-AllDocuments.pdf";
            //string path = "../../../Examplefiles/pdf/3.Krav p%c3%a5 leverant%c3%b6ren-1.pdf";
            //string path = "../../../Examplefiles/pdf/5.Kommersiella villkor-1.pdf";
            //string path = "../../../Examplefiles/pdf/6.2 Teknisk beskrivning Hus 9 utbyte av ställverk.pdf";
            //string path = "../../../Examplefiles/pdf/7.3 Föreskrifter för entreprenörer.pdf";
            //string path = "../../../Examplefiles/pdf/11.1 Byggnadsbeskrivning.pdf";
            string path = "../../../Examplefiles/pdf/ProTendering Space ship tech attatchment switch gear V1.pdf";
            ReadPdf(path);
        }

        static void WriteToFile(string text, string filePath)
        {
            Console.WriteLine("Writing ...");
            StreamWriter streamWriter = new StreamWriter(filePath, true);
            streamWriter.Write(text);
            streamWriter.Close();
            Console.WriteLine("Finished writing ... file is closed.");
        }

        static void ReadPdf(string filePath)
        {
            Dictionary<double, int> fontSizeDictionary = new Dictionary<double, int>();
            Dictionary<double, int> rowDistances = new Dictionary<double, int>();
            List<Page> readDocument = new List<Page>();

            List<double> fontSizes = new List<double>();

            string probablePageHeader = "";
            string probablePageFooter = "";
            double criteriaPercentageToDetermineHeaderAndFooter = 0.9;
            int numberOfPagesWithoutHeaderOrFooter = 1;

            double FLOATING_NUMBER_ROUNDING_ERROR_TOLERANCE = 0.01;
            Dictionary<string, int> probablePageHeaders = new Dictionary<string, int>();
            Dictionary<string, int> probablePageFooters = new Dictionary<string, int>();

            InMemoryPdfDocument inMemoryPdfDocument = new InMemoryPdfDocument();
            inMemoryPdfDocument.Content = new List<IContent>();

            Console.WriteLine("Reading Pdf ...");
            using (var stream = File.OpenRead(filePath))
            using (PdfDocument pdfDocument = PdfDocument.Open(stream))
            {
                int minimumNumberOfMatchesToConsiderHeaderOrFooter =
                    (int)Math.Floor(criteriaPercentageToDetermineHeaderAndFooter * (pdfDocument.NumberOfPages - numberOfPagesWithoutHeaderOrFooter));

                string currentParagraphText = "";
                Paragraph currentParagraph = new Paragraph();
                StyleInfo styleInfoOfPreviousBlock = new StyleInfo();
                StyleInfo styleInfoOfCurrentBlock = new StyleInfo();
                double previousLetterStartBaseLineY = -1.0;

                List<string> potentialTitleBlocks = new List<string>();

                foreach (Page page in pdfDocument.GetPages())
                {
                    //Saving every page to the in-memory-document
                    readDocument.Add(page);

                    //Get words and blocks of this page
                    var pageWords = page.GetWords();
                    var blocks = DocstrumBoundingBoxes.Instance.GetBlocks(pageWords);
                    var numberOfBlocks = blocks.Count;

                    //TODO: Where to find the title of the document?
                    //Would it be wrong to assume it is the same as the first paragraph of the first page? Or else ...
                    if (page.Number == 1)
                    {
                        //Getting the document width
                        var pageWidth = page.Width;
                        inMemoryPdfDocument.Width = pageWidth;

                        //Getting the document height
                        var pageHeight = page.Height;
                        inMemoryPdfDocument.Height = pageHeight;

                        int numberOfBlocksInTitle = Math.Min(5, blocks.Count);
                        //inMemoryPdfDocument.Title = pageWords.ToString();
                        for (int blockIndex = 0; blockIndex < numberOfBlocksInTitle; blockIndex++)
                        {
                            potentialTitleBlocks.Add(blocks[blockIndex].Text);
                        }
                    }


                    //Getting the header if any - part 1:2
                    probablePageHeader = blocks[0].Text;
                    if (probablePageHeaders.ContainsKey(probablePageHeader))
                    {
                        probablePageHeaders[probablePageHeader] += 1;
                    }
                    else
                    {
                        probablePageHeaders[probablePageHeader] = 1;
                    }


                    //Getting the footer if any - part 1:2
                    probablePageFooter = blocks[numberOfBlocks - 1].Text;
                    if (probablePageFooter.Equals(page.Number.ToString()) && numberOfBlocks >= 2)
                    {
                        probablePageFooter = blocks[numberOfBlocks - 2].Text;
                    }
                    if (probablePageFooters.ContainsKey(probablePageFooter))
                    {
                        probablePageFooters[probablePageFooter] += 1;
                    }
                    else
                    {
                        probablePageFooters[probablePageFooter] = 1;
                    }


                    IReadOnlyList<Letter> letters = page.Letters;
                    int letterIndex = 0;


                    double defaultValueOfStartBaseLineY = letters[0].StartBaseLine.Y;
                    if (Math.Abs(previousLetterStartBaseLineY - (-1.0)) < FLOATING_NUMBER_ROUNDING_ERROR_TOLERANCE)
                    {
                        previousLetterStartBaseLineY = defaultValueOfStartBaseLineY;
                    }
                    double rowDistance = 0.0;
                    bool stillSameRow = false;

                    //For all blocks of this page
                    //Do the following ...
                    for (int index = 0; index < numberOfBlocks; index++)
                    {
                        var block = blocks[index];
                        string blockText = block.Text;


                        //"Throw away" all initial empty spaces
                        while (letters[letterIndex].Value == " ")
                        {
                            letterIndex++;
                        }


                        // Jag skall plocka ut första tecknet i varje block,
                        // för att därmed kunna sätta stilinformationen för blocket ifråga
                        // (som, enligt min nuvarande hypotes, är densamma för hela blocket. Stämmer detta???)
                        Letter letter = letters[letterIndex];
                        string letterString = letter.Value;
                        styleInfoOfCurrentBlock = new StyleInfo();
                        styleInfoOfCurrentBlock.Color = letter.Color;
                        styleInfoOfCurrentBlock.FontName = letter.FontName;
                        styleInfoOfCurrentBlock.Font = letter.Font;
                        styleInfoOfCurrentBlock.FontSize = letter.FontSize;
                        styleInfoOfCurrentBlock.TextOrientation = letter.TextOrientation;


                        var letterStartBaseLine = letter.StartBaseLine;
                        var letterEndBaseLine = letter.EndBaseLine;
                        rowDistance = Math.Abs(previousLetterStartBaseLineY - letterStartBaseLine.Y);

                        //OM det är första tecknet i paragrafen -> stillSameRow = false;
                        if (currentParagraphText.Equals(""))
                        {
                            stillSameRow = false;
                        }
                        // OM det inte är första raden (dvs där y-värdet INTE = det första y-värdet) MEN skillnaden i radhöjd är försumbart liten, SÅ betraktas tecknet finnas på samma rad som föregående tecken
                        else if (rowDistance < FLOATING_NUMBER_ROUNDING_ERROR_TOLERANCE)
                        {
                            stillSameRow = true;
                        }
                        // ANNARS, dvs OM det inte är första raden OCH OM det INTE är försumbar skillnaden i radhöjd (jämfört med radhöjden för föregående tecken), SÅ betraktas tecknet befinnas på ny rad
                        else
                        {
                            stillSameRow = false;

                            if (rowDistances.ContainsKey(rowDistance))
                            {
                                rowDistances[rowDistance] += 1;
                            }
                            else
                            {
                                rowDistances[rowDistance] = 1;
                            }
                        }




                        // OM aktuellt tecken fortfarande är på samma rad som föregående tecken, SÅ konkatenera det med befintlig paragraf-sträng
                        // ANNARS, OM stilinformationen inte förändrats, SÅ konkatenera ändå tecknet med befintlig paragraf-sträng
                        //          ANNARS (dvs OM det är ny rad OCH förändrad stilinformation) SÅ handlar det om en ny paragraf
                        //                  således skall den konkatenerade paragraf-strängen OCH dess stilinformation sparas ned och paragrafen skall adderas till dokumnetets paragraf-lista
                        //                          Dessutom skall en ny paragraf skapas OCH aktuellt tecken skall starta upp/inleda denna nya paragrafs paragraf-sträng 
                        if (stillSameRow)
                        {
                            currentParagraphText += " " + blockText[0];
                        }
                        //-------------------------------------------
                        //----- Ny rad ------------------------------ 
                        //-------------------------------------------
                        // (Ny rad eller rättare sagt) ingen tidigare stilinformation OCH därmed är det således allra första raden 
                        // Starta upp en ny paragraf med detta första tecken
                        else if (styleInfoOfPreviousBlock.Empty())
                        {
                            currentParagraph = new Paragraph();
                            currentParagraphText += blockText.Trim();
                        }
                        // ANNARS: Ny rad + tidigre stilinformation + förändrad stilinformation
                        //TODO: Måste även hitta ytterligare något kriterium för att utlösa paragraf-skifte,
                        //  för fallet/situationen där stilinformationen fortfarande är densamma trots att det egentligen handlar om en ny paragraf
                        //  (Dock var det ju så här (dvs som text med oförändrad stil) som jag uppfattade definitionen av paragraf ...)
                        else if (!styleInfoOfPreviousBlock.Empty() &&
                                 !styleInfoOfPreviousBlock.Equals(styleInfoOfCurrentBlock))
                        {
                            // Ny rad OCH annorlunda stilinformation än för senaste raden
                            // Starta upp en ny paragraf samt
                            // lagra befintlig paragraftext och stilinformation 
                            currentParagraph.Text = currentParagraphText;
                            currentParagraph.StyleInfo = styleInfoOfPreviousBlock;
                            if (!(currentParagraph.Text.Trim().Equals(page.Number.ToString()) || currentParagraph.Text.Trim().Equals("")))
                            {
                                inMemoryPdfDocument.Content.Add(currentParagraph);
                                currentParagraph = new Paragraph();
                                currentParagraphText = blockText.Trim();
                            }
                        }
                        else
                        // (1) Ny rad
                        // Det finns stilinformation sedan tidigare
                        // Men informationen är inte annorlunda från föregående rad
                        // SÅLEDES betraktas denna rad som tillhörande befintlig paragraf 
                        {
                            // Dvs fortsätt addera/konkatenera text till aktuell paragraf
                            currentParagraphText += " " + blockText.Trim();
                        }

                        // OM och endast om det är sista tecknet som nu körts igenom så spara aktuell stilinformation som föregående stilinformation
                        styleInfoOfPreviousBlock = styleInfoOfCurrentBlock;

                        previousLetterStartBaseLineY = letterStartBaseLine.Y;
                        if (!stillSameRow)
                        {
                            double fontSize = letter.FontSize;
                            if (!fontSizeDictionary.ContainsKey(fontSize))
                            {
                                fontSizeDictionary[fontSize] = 1;
                            }
                            else
                            {
                                fontSizeDictionary[fontSize] += 1;
                            }
                        }

                        letterIndex += blockText.Length;
                    }
                }

                //Getting the header if any - part 2:2
                probablePageHeader = getMaxOf(probablePageHeaders);
                //TODO: Consider the first block text to be a document header and thus assign this value to that field of the document object under creation
                inMemoryPdfDocument.Header = probablePageHeader;
                string tempTitleString = String.Join(' ', potentialTitleBlocks);
                inMemoryPdfDocument.Title = tempTitleString;
                int indicatorThatPageHeaderAreEqualToTitle = 1;
                foreach (string firstPageTextBlock in potentialTitleBlocks)
                {
                    if (probablePageHeader.Contains(firstPageTextBlock))
                    {
                        indicatorThatPageHeaderAreEqualToTitle *= 0;
                    }
                }

                // ANTINGEN så är något av textblocken från första sidan med i PageHeadern 
                if (indicatorThatPageHeaderAreEqualToTitle == 0)
                {
                    inMemoryPdfDocument.Title = probablePageHeader;
                }

                // ELLER så är inget av blocken från första sidan med i PageHeadern
                // OCH då är titeln någonting annat än PageHeadern
                // Frågan är vad!
                // En sak man kan kolla, är det omvända fallet, dvs vilket av blocken från första sidan som innehåller PageHeadern
                foreach (string firstPageTextBlock in potentialTitleBlocks)
                {
                    if (firstPageTextBlock.Contains(probablePageHeader))
                    {
                        inMemoryPdfDocument.Title = firstPageTextBlock;
                    }
                }
                // TODO:
                // Eller om en kombination av textblock från första sidan innehåller PageHeadern MEN att det samtidigt gäller att PageHeadern inte innehåller något av blocken (således att PageHeadern utgör en äkta delmängd av blockens kombination av ) 
                //Getting the footer if any - part 2:2
                probablePageFooter = getMaxOf(probablePageFooters);
                int maxCounter = 0;
                int numberToCompare = probablePageFooters[probablePageFooter];
                foreach (string pageFooterKey in probablePageFooters.Keys)
                {
                    if (probablePageFooters[pageFooterKey] == numberToCompare)
                    {
                        maxCounter++;
                    }
                }
                if (maxCounter == 1)
                {
                    inMemoryPdfDocument.Footer = probablePageFooter;
                }
                else
                {
                    inMemoryPdfDocument.Footer = "None";
                }
            }

            present_the_document(inMemoryPdfDocument);
            Console.WriteLine("Finished reading ... file is closed.");
        }

        private static void present_the_document(InMemoryPdfDocument inMemoryPdfDocument)
        {
            Console.WriteLine("Dokument-titel = " + inMemoryPdfDocument.Title);
            Console.WriteLine("Dokument-header = " + inMemoryPdfDocument.Header);
            Console.WriteLine("Dokument-footer = " + inMemoryPdfDocument.Footer);
            Console.WriteLine("Dokument-bredd = " + inMemoryPdfDocument.Width);
            Console.WriteLine("Dokument-höjd = " + inMemoryPdfDocument.Height);
            List<IContent> documentContent = inMemoryPdfDocument.Content;
            int paragraphCounter = 1;
            foreach (Paragraph paragraph in documentContent)
            {
                Console.WriteLine("\nParagraf nr" + paragraphCounter + ":");
                Console.WriteLine(paragraph.Text);
                paragraphCounter++;
            }
        }

        private static string getMaxOf(Dictionary<string, int> probableItems)
        {
            int max = 0;
            string mostFrequentlyOccuringText = "";
            foreach (var probableItem in probableItems)
            {
                if (probableItem.Value > max)
                {
                    max = probableItem.Value;
                    mostFrequentlyOccuringText = probableItem.Key;
                }
            }

            return mostFrequentlyOccuringText;
        }
    }
}