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

    public static async Task CreateVoiceFiles(string[] args)
    {
        ////string[] voiceFiles = { "Twenty-nine", "Twenty-eight", "Twenty-seven", "Twenty-six", "Twenty-five", "Twenty-four", "Twenty-three", "Twenty-two", "Twenty-one", "Twenty", "Nineteen", "Eighteen", "Seventeen", "Sixteen", "Fifteen", "Fourteen", "Thirteen", "Twelve", "Eleven", "Ten", "Nine", "Eight", "Seven", "Six", "Five", "Four", "Three", "Two", "One" };
        ////string[] voiceFiles = new string[9];
        //string fileTitle = "";
        ////voiceFiles[0] = "Oh dear, just 1 life left";
        ////for (int i = 2; i < 6; i++)
        ////{
        ////    voiceFiles[i - 1] = $"You've lost a life, {i.ToString()} lives left";
        ////}
        ////fileTitle = "lives_left";
        //string[] voiceFiles = { "Level 1. Begin", "Congrats, you've made it to level 2.", "Level 3. Keep going!", "Level 4. You're doing great!", "Well done, You've reached level 5", "Splendid, level 6", "Level 7. You're on a roll!", "Level 8. Keep it up!", "Level 9. You're almost there!", "Level 10. You've made it!" };
        ////voiceFiles[0] = "Level 1. Begin";
        ////voiceFiles[1] = "Congrats, you've made it to level 2.";
        ////voiceFiles[2] = "Level 3. Keep going!";
        ////voiceFiles[3] = "Level 4. You're doing great!";
        ////voiceFiles[4] = "Well done, You've reached level 5";
        ////voiceFiles[5] = "Splendid, level 6";
        ////voiceFiles[6] = "Level 7. You're on a roll!";
        ////voiceFiles[7] = "Level 8. Keep it up!";
        ////voiceFiles[8] = "Level 9. You're almost there!";
        ////voiceFiles[9] = "Level 10. You've made it!";
        //fileTitle = "level";
        string fileTitle = "HexaPatternMatchIntro";
        string textToConvert = "Welcome to Hexa Pattern Match! Memorize the glowing yellow tiles and tap them before time runs out. Each level gets trickier with more tiles to remember. Stay sharp and beat the challenge! 3... 2... 1... GO!";

        double initialStability = 0.8;
        double finalStability = 0.3;
        double initialSimilarityBoost = 0.7; 
        double finalSimilarityBoost = 0.9;
        double initialStyle = 0.1;
        double finalStyle = 1.0;

        //int totalNumbers = voiceFiles.Length;
        //for (int i = 0; i < totalNumbers; i++)
        //{
        //    string textToConvert = voiceFiles[i];
        //    //string outputFile = $"{fileTitle}_{textToConvert.Replace(" ", "_")}.mp3";
        //    string outputFile = $"{fileTitle}.mp3";

        //    double stability = initialStability - (i * (initialStability - finalStability) / (totalNumbers - 1));
        //    double similarityBoost = initialSimilarityBoost + (i * (finalSimilarityBoost - initialSimilarityBoost) / (totalNumbers - 1));
        //    double style = initialStyle + (i * (finalStyle - initialStyle) / (totalNumbers - 1));

        //    await TextToSpeechAsync(textToConvert, outputFile, stability, similarityBoost, style);
        //}

        string outputFile = $"{fileTitle}.mp3";

        double stability = initialStability - (initialStability - finalStability);
        double similarityBoost = initialSimilarityBoost + (finalSimilarityBoost - initialSimilarityBoost);
        double style = initialStyle + (finalStyle - initialStyle);

        await TextToSpeechAsync(textToConvert, outputFile, stability, similarityBoost, style);

    }
}
