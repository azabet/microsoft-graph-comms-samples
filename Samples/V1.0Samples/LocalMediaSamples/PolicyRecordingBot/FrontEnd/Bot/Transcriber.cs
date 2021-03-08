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
        private string callId;
        private TaskCompletionSource<int> stopRecognition;

        /// <summary>Initializes a new instance of the <see cref="Transcriber"/> class.</summary>
        /// <param name="callId">Call ID.</param>
        public Transcriber(string callId)
        {
            Publisher.Publish("INFO", $"{callId} >> initializing transcriber");
            this.callId = callId;
            this.audioInputStream = AudioInputStream.CreatePushStream();
            this.audioConfig = AudioConfig.FromStreamInput(this.audioInputStream);
            this.recognizer = new SpeechRecognizer(speechConfig, this.audioConfig);
            this.stopRecognition = new TaskCompletionSource<int>();
            this.recognizer.Recognizing += (s, e) =>
            {
                Publisher.Publish("RECOGNIZING", e.Result.Text);
            };
            this.recognizer.Recognized += (s, e) =>
            {
                Publisher.Publish("RECOGNIZED", e.Result.Text);
            };
            this.recognizer.Canceled += (s, e) =>
            {
                Publisher.Publish("CANCELED", e.ToString());
                if (e.Reason == CancellationReason.Error)
                {
                    this.stopRecognition.TrySetResult(0);
                }
            };
            this.recognizer.SessionStarted += (s, e) =>
            {
                Publisher.Publish("INFO", $"{callId} transcription session {e.SessionId} started");
            };
            this.recognizer.SessionStopped += (s, e) =>
            {
                Publisher.Publish("INFO", $"{callId} transcription session {e.SessionId} stopped");
                this.stopRecognition.TrySetResult(0);
            };
            Publisher.Publish("INFO", $"{callId} << transcriber initialized");
        }

        /// <summary>Start continuous transcription.</summary>
        /// <returns>Awaitable task.</returns>
        public async Task StartTranscriptionAsync()
        {
            await this.recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);
            Publisher.Publish("INFO", $"{this.callId} transcription service started");

            // Waits for completion.
            // Use Task.WaitAny to keep the task rooted.
            Task.WaitAny(new[] { this.stopRecognition.Task });
            Publisher.Publish("INFO", $"{this.callId} transcription ended");

            await this.recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
            Publisher.Publish("INFO", $"{this.callId} transcription service stopped");
        }

        /// <summary>Stop continuous transcription.</summary>
        public void Dispose()
        {
            Publisher.Publish("INFO", $"{this.callId} >> disposing transcriber");
            try
            {
                this.stopRecognition.TrySetResult(0);
                /* this.recognizer.Dispose(); */
                this.audioConfig.Dispose();
                this.audioInputStream.Close();
                this.audioInputStream.Dispose();
                Publisher.Publish("INFO", $"{this.callId} << transcriber disposed");
            }
            catch (Exception ex)
            {
                Publisher.Publish("ERROR", $"{this.callId} error in disposing the transcriber {ex}");
            }
        }

        /// <summary>Push audio.</summary>
        /// <param name="buffer">Byte array.</param>
        public void PushAudio(byte[] buffer)
        {
            this.audioInputStream.Write(buffer);
        }
    }
}
