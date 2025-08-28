using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MakroCompare1408.Models;
using System;
using System.Threading.Tasks;

namespace MakroCompare1408.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly CodeComparisonService _comparisonService;

    [ObservableProperty]
    private string _code1 = string.Empty;

    [ObservableProperty]
    private string _code2 = string.Empty;

    [ObservableProperty]
    private string _resultText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanInteract))]
    private bool _isProcessing = false;

    [ObservableProperty]
    private double _syntaxSimilarity = 0;

    [ObservableProperty]
    private double _logicalSimilarity = 0;

    [ObservableProperty]
    private double _overallSimilarity = 0;

    public bool CanInteract => !IsProcessing;

    public MainWindowViewModel(CodeComparisonService comparisonService)
    {
        _comparisonService = comparisonService;
    }

    [RelayCommand]
    private async Task CompareCodesAsync()
    {
        if (string.IsNullOrWhiteSpace(Code1) || string.IsNullOrWhiteSpace(Code2))
        {
            ResultText = "Lütfen her iki kod alanını da doldurun.";
            return;
        }

        try
        {
            IsProcessing = true;
            ResultText = "Kodlar karşılaştırılıyor...";

            // Model kontrolü
            if (!await _comparisonService.IsModelAvailable())
            {
                ResultText = "Hata: DeepSeek modeli bulunamadı. Ollama'nın çalıştığından ve modelin yüklü olduğundan emin olun.";
                return;
            }

            var result = await _comparisonService.CompareCodesAsync(Code1, Code2);

            SyntaxSimilarity = result.SyntaxSimilarity;
            LogicalSimilarity = result.LogicalSimilarity;
            OverallSimilarity = result.OverallSimilarity;

            ResultText = $"✅ Karşılaştırma Tamamlandı!\n\n" +
                        $"📝 Yazım Benzerliği: %{SyntaxSimilarity:F1}\n" +
                        $"🧠 Mantıksal Benzerlik: %{LogicalSimilarity:F1}\n" +
                        $"📊 Genel Benzerlik: %{OverallSimilarity:F1}";
        }
        catch (Exception ex)
        {
            ResultText = $"❌ Hata: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
        }
    }

    [RelayCommand]
    private void ClearAll()
    {
        Code1 = string.Empty;
        Code2 = string.Empty;
        ResultText = string.Empty;
        SyntaxSimilarity = 0;
        LogicalSimilarity = 0;
        OverallSimilarity = 0;
    }
}
