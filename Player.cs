using CSCore;
using CSCore.SoundOut;
using System;

namespace radiotray
{
    public sealed class Player
    {
        private readonly ISoundOut soundOut;
        private IWaveSource soundSource = null;

        public Player()
        {
            soundOut = GetSoundOut();
            soundOut.Stopped += (object sender, PlaybackStoppedEventArgs e) =>
            {
                if (Stopped != null)
                    Stopped(this, e);
            };
        }

        private static ISoundOut GetSoundOut()
        {
            if (WasapiOut.IsSupportedOnCurrentPlatform)
                return new WasapiOut();
            else
                return new DirectSoundOut();
        }

        private static IWaveSource GetSoundSource(string uri)
        {
            return new CSCore.MediaFoundation.MediaFoundationDecoder(uri);
        }

        public Exception Play(string uri)
        {
            Stop();
            try
            {
                soundSource = GetSoundSource(uri);
                soundOut.Initialize(soundSource);
                soundOut.Play();
                return null;
            }
            catch (Exception e)
            {
                if (soundSource != null)
                {
                    soundSource.Dispose();
                    soundSource = null;
                }
                return e;
            }
        }

        public void Stop()
        {
            soundOut.Stop();
            if (soundSource != null)
            {
                soundSource.Dispose();
                soundSource = null;
            }
        }

        public void Dispose()
        {
            Stop();
            soundOut.Dispose();
        }

        public event EventHandler<PlaybackStoppedEventArgs> Stopped;
    }
}
