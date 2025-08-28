using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.Http;

namespace MakroCompare1408.Models;

public class OllamaChat
{
    private readonly HttpClient _httpClient;
    private const string OLLAMA_URL = "http://localhost:11434";

    public OllamaChat(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> GetResponse(string model, string prompt)
    {
        try
        {
            var request = new
            {
                model = model,
                prompt = prompt,
                stream = false,
                options = new
                {
                    temperature = 0.0,
                    top_p = 0.1,
                    top_k = 1,
                    num_predict = 5
                }
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{OLLAMA_URL}/api/generate", content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var ollamaResponse = JsonSerializer.Deserialize<OllamaResponse>(responseContent);

            var result = ollamaResponse?.Response?.Trim() ?? string.Empty;
            Console.WriteLine($"AI Response: '{result}'");
            return result;
        }
        catch (Exception ex)
        {
            throw new Exception($"Ollama API Hatası: {ex.Message}");
        }
    }

    public async Task<bool> IsModelAvailable(string model)
    {
        try
        {
            var testRequest = new
            {
                model = model,
                prompt = "test",
                stream = false
            };

            var json = JsonSerializer.Serialize(testRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{OLLAMA_URL}/api/generate", content);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}

public class OllamaResponse
{
    public string Response { get; set; } = string.Empty;
    public bool Done { get; set; }
}
