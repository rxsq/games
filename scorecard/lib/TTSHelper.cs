using Google.Cloud.TextToSpeech.V1;
using Google.Api.Gax.Grpc;
using Grpc.Core;
using System;
using System.IO;
using NAudio.Wave;
using Grpc.Auth;

public class TTSHelper
{
    private TextToSpeechClient client;

    public TTSHelper(string apiKey)
    {
        //var channel = new GrpcChannelOptions
        //{

        //    Credentials = ChannelCredentials.Insecure,
        //    PrimaryUserAgent = null,
        //    ApiKey = apiKey
        //};


        //var channelCredentials = ChannelCredentials.Create(new SslCredentials(), GoogleGrpcCredentials.FromApiKey(apiKey));
        //var channel = new Channel("texttospeech.googleapis.com", channelCredentials);


        client = new TextToSpeechClientBuilder
        {
            GrpcAdapter = GrpcCoreAdapter.Instance,
            JsonCredentials = apiKey
        }.Build();
    }

    public void SpeakText(string text, bool useSsml = false)
    {
        var input = new SynthesisInput
        {
            Text = useSsml ? null : text,
            Ssml = useSsml ? text : null
        };

        var voiceSelection = new VoiceSelectionParams
        {
            LanguageCode = "en-US",
            SsmlGender = SsmlVoiceGender.Female
        };

        var audioConfig = new AudioConfig
        {
            AudioEncoding = AudioEncoding.Mp3
        };

        var response = client.SynthesizeSpeech(input, voiceSelection, audioConfig);

        string filePath = "speech.mp3";
        using (var output = File.Create(filePath))
        {
            response.AudioContent.WriteTo(output);
        }

        using (var audioFile = new AudioFileReader(filePath))
        using (var outputDevice = new WaveOutEvent())
        {
            outputDevice.Init(audioFile);
            outputDevice.Play();
            while (outputDevice.PlaybackState == PlaybackState.Playing)
            {
                System.Threading.Thread.Sleep(1000);
            }
        }
    }
}
