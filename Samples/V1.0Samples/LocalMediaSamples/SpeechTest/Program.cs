using System;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;

namespace SpeechTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Program started");
            RecognitionWithPushAudioStreamAsync().Wait();
            Console.WriteLine("Program finished" );
        }

        public static async Task RecognitionWithPushAudioStreamAsync()
        {
            // Creates an instance of a speech config with specified subscription key and service region.
            string KEY = Environment.GetEnvironmentVariable("AZURE_KEY");
            string REGION = Environment.GetEnvironmentVariable("AZURE_REGION") ?? "eastus";
            var speechConfig = SpeechConfig.FromSubscription(KEY, REGION);
            Console.WriteLine($"speechConfig: {speechConfig}");

            var stopRecognition = new TaskCompletionSource<int>();

            // Create a push stream
            using (var pushStream = AudioInputStream.CreatePushStream())
            {
                using (var audioInput = AudioConfig.FromStreamInput(pushStream))
                {
                    // Creates a speech recognizer using audio stream input.
                    using (var recognizer = new SpeechRecognizer(speechConfig, audioInput))
                    {
                        // Subscribes to events.
                        recognizer.Recognizing += (s, e) =>
                        {
                            Console.WriteLine($"RECOGNIZING: Text={e.Result.Text} {e} {e.Result}");
                        };

                        recognizer.Recognized += (s, e) =>
                        {
                            if (e.Result.Reason == ResultReason.RecognizedSpeech)
                            {
                                Console.WriteLine($"RECOGNIZED: Text={e.Result.Text} {e} {e.Result}");
                            }
                            else if (e.Result.Reason == ResultReason.NoMatch)
                            {
                                Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                            }
                        };

                        recognizer.Canceled += (s, e) =>
                        {
                            Console.WriteLine($"CANCELED: Reason={e.Reason}");

                            if (e.Reason == CancellationReason.Error)
                            {
                                Console.WriteLine($"CANCELED: ErrorCode={e.ErrorCode}");
                                Console.WriteLine($"CANCELED: ErrorDetails={e.ErrorDetails}");
                                Console.WriteLine($"CANCELED: Did you update the subscription info?");
                            }

                            stopRecognition.TrySetResult(0);
                        };

                        recognizer.SessionStarted += (s, e) =>
                        {
                            Console.WriteLine("\nSession started event.");
                        };

                        recognizer.SessionStopped += (s, e) =>
                        {
                            Console.WriteLine("\nSession stopped event.");
                            Console.WriteLine("\nStop recognition.");
                            stopRecognition.TrySetResult(0);
                        };

                        // Starts continuous recognition. Uses StopContinuousRecognitionAsync() to stop recognition.
                        Console.WriteLine("StartContinuousRecognitionAsync");
                        await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);

                        // open and read the wave file and push the buffers into the recognizer
                        using (BinaryAudioStreamReader reader = Helper.CreateWavReader(@"whatstheweatherlike.wav"))
                        {
                            byte[] buffer = new byte[1000];
                            while (true)
                            {
                                var readSamples = reader.Read(buffer, (uint)buffer.Length);
                                if (readSamples == 0)
                                {
                                    break;
                                }
                                pushStream.Write(buffer, readSamples);
                            }
                        }
                        pushStream.Close();

                        // Waits for completion.
                        // Use Task.WaitAny to keep the task rooted.
                        Console.WriteLine("Task.WaitAny ");
                        Task.WaitAny(new[] { stopRecognition.Task });

                        // Stops recognition.
                        Console.WriteLine("StopContinuousRecognitionAsync");
                        await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
                    }
                }
            }
        }
    }
}
