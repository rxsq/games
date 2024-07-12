using Google.Cloud.Speech.V1;
using System;
using System.IO;

public class STTHelper
{
    private SpeechClient speechClient;

    public STTHelper(string credentialsPath)
    {
        Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", credentialsPath);
        speechClient = SpeechClient.Create();
    }

    public string TranscribeAudio(string audioFilePath)
    {
        var response = speechClient.Recognize(new RecognitionConfig
        {
            Encoding = RecognitionConfig.Types.AudioEncoding.Linear16,
            SampleRateHertz = 16000,
            LanguageCode = "en-US"
        }, RecognitionAudio.FromFile(audioFilePath));

        foreach (var result in response.Results)
        {
            foreach (var alternative in result.Alternatives)
            {
                return alternative.Transcript;
            }
        }

        return null;
    }
}
