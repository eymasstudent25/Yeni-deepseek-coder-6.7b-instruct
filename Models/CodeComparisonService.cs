using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MakroCompare1408.Models
{
    public class CodeComparisonService
    {
        private readonly OllamaChat _ollamaChat;
        private const string MODEL_NAME = "deepseek-coder:6.7b-instruct";

        public CodeComparisonService(OllamaChat ollamaChat)
        {
            _ollamaChat = ollamaChat;
        }

        public async Task<CodeComparisonResult> CompareCodesAsync(string code1, string code2)
        {
            // Aynı kod geldiyse direkt %100 benzer
            if (code1.Trim() == code2.Trim())
            {
                return new CodeComparisonResult
                {
                    SyntaxSimilarity = 100,
                    LogicalSimilarity = 100,
                    OverallSimilarity = 100
                };
            }

            // Paralel AI çağrıları
            var syntaxTask = GetSyntaxSimilarityAsync(code1, code2);
            var logicalTask = GetLogicalSimilarityAsync(code1, code2);

            await Task.WhenAll(syntaxTask, logicalTask);

            var syntaxSimilarity = await syntaxTask;
            var logicalSimilarity = await logicalTask;

            // Mantıksal benzerliğe daha fazla ağırlık verdik (%70 mantık, %30 sözdizim)
            var overallSimilarity = (logicalSimilarity * 0.7) + (syntaxSimilarity * 0.3);

            return new CodeComparisonResult
            {
                SyntaxSimilarity = syntaxSimilarity,
                LogicalSimilarity = logicalSimilarity,
                OverallSimilarity = overallSimilarity
            };
        }

        private async Task<double> GetSyntaxSimilarityAsync(string code1, string code2)
        {
            var prompt = $@"İki VBA kodunun yazımsal (syntax) benzerliğini değerlendir.

Kod1:
{code1}

Kod2:
{code2}

Bu iki kodun yazımsal benzerliği yüzde kaç? 
Sadece 0-100 arası bir sayı yaz. 
Sayıyı rakamla yaz (örn: 85), yazıyla yazma. 
Başka hiçbir şey yazma.";

            return await GetNumericResponseFromModel(prompt);
        }

        private async Task<double> GetLogicalSimilarityAsync(string code1, string code2)
        {
            var prompt = $@"İki VBA kodunun mantıksal (logical) benzerliğini değerlendir.Mantıksal benzerlikten kastım;kodların yaptıkları amaç ne kadar benziyo bunun yüzdelik değerini tutalı bir şekilde ver

Kod1:
{code1}

Kod2:
{code2}

Bu iki kodun mantıksal benzerliği yüzde kaç? 
Sadece 0-100 arası bir sayı yaz. 
Sayıyı rakamla yaz (örn: 85), yazıyla yazma. 
Başka hiçbir şey yazma.";

            return await GetNumericResponseFromModel(prompt);
        }

        /// <summary>
        /// AI’den gelen cevabı alır ve sayıya çevirir (0-100 arası).
        /// </summary>
        private async Task<double> GetNumericResponseFromModel(string prompt)
        {
            var response = await _ollamaChat.GetResponse(MODEL_NAME, prompt);
            Console.WriteLine($"AI raw response: '{response}'");

            // Regex ile ilk sayıyı yakala
            var match = Regex.Match(response, @"\d+(\.\d+)?");
            if (match.Success)
            {
                if (double.TryParse(match.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double score))
                    return Math.Clamp(score, 0, 100);
            }

            // Fallback: tüm sayıları dene
            var matches = Regex.Matches(response, @"\d+(\.\d+)?");
            foreach (Match m in matches)
            {
                if (double.TryParse(m.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double score))
                {
                    if (score >= 0 && score <= 100)
                        return score; // ilk uygun sayı
                }
            }

            // Hâlâ sayı yoksa hata fırlat
            throw new InvalidOperationException($"AI yanıtı sayıya çevrilemedi: '{response}'");
        }

        public async Task<bool> IsModelAvailable()
        {
            return await _ollamaChat.IsModelAvailable(MODEL_NAME);
        }
    }

    public class CodeComparisonResult
    {
        public double SyntaxSimilarity { get; set; }
        public double LogicalSimilarity { get; set; }
        public double OverallSimilarity { get; set; }
    }
}
