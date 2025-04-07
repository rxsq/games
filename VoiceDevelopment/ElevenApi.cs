using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;

class ElevenLabsTTS
{
    private static readonly string apiKey = "sk_2816c48e389d6ab75030959b4a1581391b8ab0cba2aea603";
    private static readonly string apiUrl = "https://api.elevenlabs.io/v1/text-to-speech/N2lVS1w4EtoT3dr4eOWO?enable_logging=true";

    public static async Task TextToSpeechAsync(string text, string outputFile, double stability, double similarityBoost, double style)
    {
        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("xi-api-key", apiKey);

            var requestBody = new
            {
                text = text,
                output_format = "mp3_22050_32",
                voice_settings = new

                {
                    stability = stability,
                    similarity_boost = similarityBoost,
                    style = style,
                    use_speaker_boost = true
                },
                model_id = "eleven_turbo_v2"
            };

            var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

            var response = await client.PostAsync(apiUrl, content);

            if (response.IsSuccessStatusCode)
            {
                byte[] audioBytes = await response.Content.ReadAsByteArrayAsync();
                File.WriteAllBytes(outputFile, audioBytes);
                Console.WriteLine("Audio file saved to " + outputFile);
            }
            else
            {
                string errorMessage = await response.Content.ReadAsStringAsync();
                Console.WriteLine("Error: " + errorMessage);
            }
        }
    }

    public static async Task CreateVoiceFiles(string fileTitle, string textToConvert)
    {
        double initialStability = 0.8;
        double finalStability = 0.3;
        double initialSimilarityBoost = 0.7;
        double finalSimilarityBoost = 0.9;
        double initialStyle = 0.1;
        double finalStyle = 1.0;

        string outputFile = $"{fileTitle}.mp3";

        double stability = initialStability - (initialStability - finalStability);
        double similarityBoost = initialSimilarityBoost + (finalSimilarityBoost - initialSimilarityBoost);
        double style = initialStyle + (finalStyle - initialStyle);

        await TextToSpeechAsync(textToConvert, outputFile, stability, similarityBoost, style);
    }
}
