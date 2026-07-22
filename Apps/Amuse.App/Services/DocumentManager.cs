using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TensorStack.WPF.Services;
using UglyToad.PdfPig;

namespace Amuse.App.Services
{
    public static class DocumentManager
    {
        private static readonly DefaultDocumentParser DefaultParser = new ();
        private static readonly Dictionary<string, IDocumentParser> DocumentParsers = new(StringComparer.OrdinalIgnoreCase)
        {  
            {".pdf", new PdfDocumentPParser() },
            {".docx", new DocxDocumentParser() }
        };


        /// <summary>
        /// Parse the document into PlainText
        /// </summary>
        /// <param name="filename">The filename.</param>
        public static async Task<string> ParseAsync(string filename)
        {
            try
            {
                var extension = Path.GetExtension(filename);
                if (DocumentParsers.TryGetValue(extension, out var documentParser))
                    return await documentParser.ParseAsync(filename);

                return await DefaultParser.ParseAsync(filename);
            }
            catch (Exception ex)
            {
                await DialogService.ShowErrorAsync("Parse Error", $"Failed to parse document.\n {ex.Message}");
                return default;
            }
        }
    }


    public interface IDocumentParser
    {
        Task<string> ParseAsync(string filename);
    }


    public sealed class DefaultDocumentParser : IDocumentParser
    {
        public async Task<string> ParseAsync(string filename)
        {
            if (!File.Exists(filename))
                return default;

            var textContent = await File.ReadAllTextAsync(filename);
            if (string.IsNullOrEmpty(textContent))
                return default;

            return textContent;
        }
    }


    public sealed class PdfDocumentPParser : IDocumentParser
    {
        public async Task<string> ParseAsync(string filename)
        {
            return await Task.Run(() =>
            {
                var parsedResult = new StringBuilder();
                if (!File.Exists(filename))
                    return parsedResult.ToString();

                using (var document = PdfDocument.Open(filename))
                {
                    foreach (var page in document.GetPages())
                    {
                        var text = page.Text?.Trim();
                        if (string.IsNullOrWhiteSpace(text))
                            continue;

                        parsedResult.AppendLine(text);
                        parsedResult.AppendLine();
                    }
                }
                return parsedResult.ToString();
            });
        }
    }


    public sealed class DocxDocumentParser : IDocumentParser
    {
        public async Task<string> ParseAsync(string filename)
        {
            return await Task.Run(() =>
            {
                using (var document = WordprocessingDocument.Open(filename, false))
                {
                    var parsedResult = new StringBuilder();
                    var documentBody = document.MainDocumentPart?.Document.Body;
                    if (documentBody == null)
                        return parsedResult.ToString();

                    foreach (var element in documentBody.Elements())
                    {
                        switch (element)
                        {
                            case Paragraph paragraph:
                                var text = paragraph.InnerText.Trim();
                                if (!string.IsNullOrWhiteSpace(text))
                                {
                                    var style = paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
                                    if (style?.StartsWith("Heading") == true)
                                    {
                                        if (int.TryParse(style.Replace("Heading", ""), out var level))
                                        {
                                            parsedResult.AppendLine($"{new string('#', level)} {text}");
                                        }
                                    }
                                    else
                                    {
                                        parsedResult.AppendLine(text);
                                    }
                                    parsedResult.AppendLine();
                                }
                                break;

                            case Table table:
                                WriteTable(parsedResult, table);
                                break;
                        }
                    }
                    return parsedResult.ToString();
                }
            });
        }


        private static void WriteTable(StringBuilder builder, Table table)
        {
            var rows = table.Elements<TableRow>().ToList();
            if (rows.Count == 0)
                return;

            var first = rows[0]
                .Elements<TableCell>()
                .Select(c => c.InnerText.Trim())
                .ToList();

            builder.AppendLine($"| {string.Join(" | ", first)} |");
            builder.AppendLine($"| {string.Join(" | ", first.Select(_ => "---"))} |");
            foreach (var row in rows.Skip(1))
            {
                var cells = row.Elements<TableCell>()
                               .Select(c => c.InnerText.Trim());
                builder.AppendLine($"| {string.Join(" | ", cells)} |");
            }
            builder.AppendLine();
        }
    }

}