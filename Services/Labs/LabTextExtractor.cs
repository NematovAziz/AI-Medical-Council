using System.Globalization;
using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace AI.MedicalCouncil.Services.Labs;

public record ExtractedText(string Text, bool IsImage, string Method);

/// <summary>Turns an uploaded file into plain text. Images are passed through for vision analysis.</summary>
public static class LabTextExtractor
{
    public static ExtractedText Extract(byte[] bytes, string fileName, string contentType)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();

        if (ext is ".png" or ".jpg" or ".jpeg" or ".webp" || contentType.StartsWith("image/"))
            return new ExtractedText(string.Empty, true, "Rasm · vision");

        if (ext == ".pdf" || contentType.Contains("pdf"))
        {
            try
            {
                var text = ReadPdf(bytes);
                return string.IsNullOrWhiteSpace(text)
                    ? new ExtractedText(string.Empty, true, "PDF · matnsiz, vision kerak")
                    : new ExtractedText(text, false, "PDF matn qatlami");
            }
            catch
            {
                return new ExtractedText(string.Empty, true, "PDF o'qilmadi");
            }
        }

        var raw = Encoding.UTF8.GetString(bytes);
        return new ExtractedText(raw, false, ext == ".csv" ? "CSV" : "Matn fayl");
    }

    /// <summary>
    /// Rebuilds visual lines from word positions. PdfPig returns words, not rows, and the lab
    /// parser is line-based — so words sharing a baseline are joined and rows are ordered top-down.
    /// </summary>
    private static string ReadPdf(byte[] bytes)
    {
        var sb = new StringBuilder();
        using var doc = PdfDocument.Open(bytes);

        foreach (var page in doc.GetPages())
        {
            var words = page.GetWords().ToList();
            if (words.Count == 0)
            {
                sb.AppendLine(page.Text);
                continue;
            }

            // group by baseline with a tolerance, so slightly uneven glyphs stay on one row
            var lines = new List<(double Y, List<Word> Words)>();

            foreach (var word in words)
            {
                var y = word.BoundingBox.Bottom;
                var line = lines.FirstOrDefault(l => Math.Abs(l.Y - y) <= 3.0);

                if (line.Words is null)
                {
                    lines.Add((y, new List<Word> { word }));
                }
                else
                {
                    line.Words.Add(word);
                }
            }

            foreach (var line in lines.OrderByDescending(l => l.Y))
            {
                var ordered = line.Words.OrderBy(w => w.BoundingBox.Left).Select(w => w.Text);
                sb.AppendLine(string.Join(" ", ordered));
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }
}
