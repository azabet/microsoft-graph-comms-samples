// <copyright file="Transcriber.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Sample.PolicyRecordingBot.FrontEnd.Bot
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.CognitiveServices.Speech;
    using Microsoft.CognitiveServices.Speech.Audio;

    /// <summary>Transcribe audio to text.</summary>
    public class Transcriber
    {
        private static string key = Service.Instance.Configuration.SpeechSubscription;
        private static SpeechConfig speechConfig = SpeechConfig.FromSubscription(key, "eastus");
        private PushAudioInputStream audioInputStream;
        private AudioConfig audioConfig;
        private SpeechRecognizer recognizer;
        private string id;
        private string speakerName;
        private TaskCompletionSource<int> stopRecognition;

        /// <summary>Initializes a new instance of the <see cref="Transcriber"/> class.</summary>
        /// <param name="id">ID to be used for logging.</param>
        public Transcriber(string id)
        {
            Publisher.Publish("INFO", $"{id} >>> initializing transcriber");
            this.id = id;
            this.audioInputStream = AudioInputStream.CreatePushStream();
            this.audioConfig = AudioConfig.FromStreamInput(this.audioInputStream);
            this.recognizer = new SpeechRecognizer(speechConfig, this.audioConfig);
            this.stopRecognition = new TaskCompletionSource<int>();
            this.recognizer.Recognizing += (s, e) =>
            {
                Publisher.Publish("RECOGNIZING", $"{this.speakerName}: {e.Result.Text}");
            };
            this.recognizer.Recognized += (s, e) =>
            {
                Publisher.Publish("RECOGNIZED", $"{this.speakerName}: {e.Result.Text}");
            };
            this.recognizer.Canceled += (s, e) =>
            {
                Publisher.Publish("INFO", $"{id} *** cancelled transcription  {e}");
                if (e.Reason == CancellationReason.Error)
                {
                    this.stopRecognition.TrySetResult(0);
                }
            };
            this.recognizer.SessionStarted += (s, e) =>
            {
                Publisher.Publish("INFO", $"{id} *** started transcription session {e.SessionId}");
            };
            this.recognizer.SessionStopped += (s, e) =>
            {
                Publisher.Publish("INFO", $"{id} *** stopped transcription session {e.SessionId}");
                this.stopRecognition.TrySetResult(0);
            };
            var success = this.recognizer.StartContinuousRecognitionAsync().Wait(5000);
            if (success)
            {
                Publisher.Publish("INFO", $"{id} >>> transcriber initialized");
            }
            else
            {
                Publisher.Publish("ERROR", $"{id} >>> transcriber timed out");
                throw new TimeoutException();
            }
        }

        /// <summary>Dispose the transcriber.</summary>
        public void Dispose()
        {
            Publisher.Publish("INFO", $"{this.id} <<< disposing transcriber");
            try
            {
                this.stopRecognition.TrySetResult(0);
                this.audioConfig.Dispose();
                this.audioInputStream.Close();
                this.audioInputStream.Dispose();
                this.recognizer.Dispose();
                Publisher.Publish("INFO", $"{this.id} <<< transcriber disposed");
            }
            catch (Exception ex)
            {
                Publisher.Publish("ERROR", $"{this.id} error in disposing the transcriber {ex}");
            }
        }

        /// <summary>Push audio.</summary>
        /// <param name="speakerName">Name.</param>
        /// <param name="buffer">Byte array.</param>
        public void PushAudio(string speakerName, byte[] buffer)
        {
            this.speakerName = speakerName;
            this.audioInputStream.Write(buffer);
        }
    }
}
