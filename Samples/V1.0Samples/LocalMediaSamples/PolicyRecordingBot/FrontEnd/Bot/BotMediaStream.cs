// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BotMediaStream.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>
// <summary>
//   The bot media stream.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sample.PolicyRecordingBot.FrontEnd.Bot
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;
    using Microsoft.Graph.Communications.Calls.Media;
    using Microsoft.Graph.Communications.Common;
    using Microsoft.Graph.Communications.Common.Telemetry;
    using Microsoft.Skype.Bots.Media;
    using Microsoft.Skype.Internal.Media.Services.Common;

    /// <summary>
    /// Class responsible for streaming audio and video.
    /// </summary>
    public class BotMediaStream : ObjectRootDisposable
    {
        private readonly IAudioSocket audioSocket;
        private readonly IVideoSocket vbssSocket;
        private readonly List<IVideoSocket> videoSockets;
        private readonly ILocalMediaSession mediaSession;
        private Transcriber transcriber;

        /// <summary>
        /// Initializes a new instance of the <see cref="BotMediaStream"/> class.
        /// </summary>
        /// <param name="mediaSession">The media session.</param>
        /// <param name="logger">Graph logger.</param>
        /// <param name="transcriber">Transcriber instance for this call.</param>
        /// <exception cref="InvalidOperationException">Throws when no audio socket is passed in.</exception>
        public BotMediaStream(ILocalMediaSession mediaSession, IGraphLogger logger, Transcriber transcriber)
            : base(logger)
        {
            Publisher.Publish("INFO", $"{mediaSession.MediaSessionId} >>> initializing media stream bot");
            Publisher.Publish("DEBUG", mediaSession.GetMediaConfiguration().ToString());
            ArgumentVerifier.ThrowOnNullArgument(mediaSession, nameof(mediaSession));
            ArgumentVerifier.ThrowOnNullArgument(logger, nameof(logger));

            this.mediaSession = mediaSession;
            this.transcriber = transcriber;

            // Subscribe to the audio media.
            this.audioSocket = mediaSession.AudioSocket;
            if (this.audioSocket == null)
            {
                throw new InvalidOperationException("A mediaSession needs to have at least an audioSocket");
            }

            this.audioSocket.AudioMediaReceived += this.OnAudioMediaReceived;

            // Subscribe to the video media.
            this.videoSockets = this.mediaSession.VideoSockets?.ToList();
            if (this.videoSockets?.Any() == true)
            {
                this.videoSockets.ForEach(videoSocket => videoSocket.VideoMediaReceived += this.OnVideoMediaReceived);
            }

            // Subscribe to the VBSS media.
            this.vbssSocket = this.mediaSession.VbssSocket;
            if (this.vbssSocket != null)
            {
                this.mediaSession.VbssSocket.VideoMediaReceived += this.OnVbssMediaReceived;
            }

            _ = transcriber.StartTranscriptionAsync();
            Publisher.Publish("INFO", $"{mediaSession.MediaSessionId} <<< media stream bot initialized");
        }

        /// <summary>
        /// Subscription for video and vbss.
        /// </summary>
        /// <param name="mediaType">vbss or video.</param>
        /// <param name="mediaSourceId">The video source Id.</param>
        /// <param name="videoResolution">The preferred video resolution.</param>
        /// <param name="socketId">Socket id requesting the video. For vbss it is always 0.</param>
        public void Subscribe(MediaType mediaType, uint mediaSourceId, VideoResolution videoResolution, uint socketId = 0)
        {
            try
            {
                this.ValidateSubscriptionMediaType(mediaType);

                this.GraphLogger.Info($"Subscribing to the video source: {mediaSourceId} on socket: {socketId} with the preferred resolution: {videoResolution} and mediaType: {mediaType}");
                if (mediaType == MediaType.Vbss)
                {
                    if (this.vbssSocket == null)
                    {
                        this.GraphLogger.Warn($"vbss socket not initialized");
                    }
                    else
                    {
                        this.vbssSocket.Subscribe(videoResolution, mediaSourceId);
                    }
                }
                else if (mediaType == MediaType.Video)
                {
                    if (this.videoSockets == null)
                    {
                        this.GraphLogger.Warn($"video sockets were not created");
                    }
                    else
                    {
                        this.videoSockets[(int)socketId].Subscribe(videoResolution, mediaSourceId);
                    }
                }
            }
            catch (Exception ex)
            {
                this.GraphLogger.Error(ex, $"Video Subscription failed for the socket: {socketId} and MediaSourceId: {mediaSourceId} with exception");
            }
        }

        /// <summary>
        /// Unsubscribe to video.
        /// </summary>
        /// <param name="mediaType">vbss or video.</param>
        /// <param name="socketId">Socket id. For vbss it is always 0.</param>
        public void Unsubscribe(MediaType mediaType, uint socketId = 0)
        {
            try
            {
                this.ValidateSubscriptionMediaType(mediaType);

                this.GraphLogger.Info($"Unsubscribing to video for the socket: {socketId} and mediaType: {mediaType}");

                if (mediaType == MediaType.Vbss)
                {
                    this.vbssSocket?.Unsubscribe();
                }
                else if (mediaType == MediaType.Video)
                {
                    this.videoSockets[(int)socketId]?.Unsubscribe();
                }
            }
            catch (Exception ex)
            {
                this.GraphLogger.Error(ex, $"Unsubscribing to video failed for the socket: {socketId} with exception");
            }
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            Publisher.Publish("INFO", $"{this.mediaSession.MediaSessionId} >>> disposing media stream bot");
            base.Dispose(disposing);

            this.audioSocket.AudioMediaReceived -= this.OnAudioMediaReceived;

            if (this.videoSockets?.Any() == true)
            {
                this.videoSockets.ForEach(videoSocket => videoSocket.VideoMediaReceived -= this.OnVideoMediaReceived);
            }

            // Subscribe to the VBSS media.
            if (this.vbssSocket != null)
            {
                this.mediaSession.VbssSocket.VideoMediaReceived -= this.OnVbssMediaReceived;
            }

            Publisher.Publish("INFO", $"{this.mediaSession.MediaSessionId} <<< media stream bot disposed");
        }

        /// <summary>
        /// Ensure media type is video or VBSS.
        /// </summary>
        /// <param name="mediaType">Media type to validate.</param>
        private void ValidateSubscriptionMediaType(MediaType mediaType)
        {
            if (mediaType != MediaType.Vbss && mediaType != MediaType.Video)
            {
                throw new ArgumentOutOfRangeException($"Invalid mediaType: {mediaType}");
            }
        }

        /// <summary>
        /// Receive audio from subscribed participant.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The audio media received arguments.
        /// </param>
        private void OnAudioMediaReceived(object sender, AudioMediaReceivedEventArgs e)
        {
            // Publisher.Publish("DEBUG", $"Received audio {e.Buffer.AudioFormat}, Length={e.Buffer.Length}, IsSilence={e.Buffer.IsSilence}");
            try
            {
                byte[] buffer = new byte[e.Buffer.Length];
                Marshal.Copy(e.Buffer.Data, buffer, 0, (int)e.Buffer.Length);
                this.transcriber.PushAudio(buffer);
            }
            catch (Exception ex)
            {
                Publisher.Publish("ERROR", $"Caught an exception while processing the audio buffer {ex}");
            }
            finally
            {
                e.Buffer.Dispose();
            }
        }

        /// <summary>
        /// Receive video from subscribed participant.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The video media received arguments.
        /// </param>
        private void OnVideoMediaReceived(object sender, VideoMediaReceivedEventArgs e)
        {
            this.GraphLogger.Info($"[{e.SocketId}]: Received Video: [VideoMediaReceivedEventArgs(Data=<{e.Buffer.Data}>, Length={e.Buffer.Length}, Timestamp={e.Buffer.Timestamp}, Width={e.Buffer.VideoFormat.Width}, Height={e.Buffer.VideoFormat.Height}, ColorFormat={e.Buffer.VideoFormat.VideoColorFormat}, FrameRate={e.Buffer.VideoFormat.FrameRate})]");
            Publisher.Publish("VIDEO", e.ToString());

            // TBD: Policy Recording bots can record the Video here
            e.Buffer.Dispose();
        }

        /// <summary>
        /// Receive vbss from subscribed participant.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The video media received arguments.
        /// </param>
        private void OnVbssMediaReceived(object sender, VideoMediaReceivedEventArgs e)
        {
            this.GraphLogger.Info($"[{e.SocketId}]: Received VBSS: [VideoMediaReceivedEventArgs(Data=<{e.Buffer.Data}>, Length={e.Buffer.Length}, Timestamp={e.Buffer.Timestamp}, Width={e.Buffer.VideoFormat.Width}, Height={e.Buffer.VideoFormat.Height}, ColorFormat={e.Buffer.VideoFormat.VideoColorFormat}, FrameRate={e.Buffer.VideoFormat.FrameRate})]");
            Publisher.Publish("VBSS", e.ToString());

            // TBD: Policy Recording bots can record the VBSS here
            e.Buffer.Dispose();
        }
    }
}
