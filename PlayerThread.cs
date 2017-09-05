using CSCore.SoundOut;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace radiotray
{
    public class PlayerThread
    {
        public class PlaybackUriEventArgs : EventArgs
        {
            public PlaybackUriEventArgs(string uri)
            {
                this.Uri = uri;
            }

            public string Uri { get; private set; }
        }

        private sealed class PlayerOperaion
        {
            public enum PlayerAction
            {
                PLAY,
                STOP,
                HALT
            }

            public PlayerOperaion(PlayerAction action) : this(action, null) { }

            public PlayerOperaion(PlayerAction action, string uri)
            {
                this.Action = action;
                this.Uri = uri;
            }

            public PlayerAction Action { get; private set; }
            public string Uri { get; private set; }
        };

        private readonly Thread thread;
        private readonly ManualResetEvent waitHandle = new ManualResetEvent(false);
        private readonly ConcurrentStack<PlayerOperaion> operations = new ConcurrentStack<PlayerOperaion>();

        public PlayerThread()
        {
            thread = new Thread((Object obj) => { (obj as PlayerThread).DoWork(); });
            thread.Start(this);
        }

        private void DoWork()
        {
            bool halting = false;

            Player player = new Player();
            player.Stopped += (object sender, PlaybackStoppedEventArgs e) =>
            {
                if (halting)
                    return;

                IsPlaying = false;
                if (Stopped != null)
                {
                    Stopped(this, e);
                }
            };

            while (true)
            {
                waitHandle.WaitOne();
                waitHandle.Reset();
                PlayerOperaion operation;
                while (operations.TryPop(out operation))
                {
                    if (operation.Action == PlayerOperaion.PlayerAction.HALT)
                        break;

                    switch (operation.Action)
                    {
                        case PlayerOperaion.PlayerAction.PLAY:
                            Exception e = player.Play(operation.Uri);
                            if (e == null)
                            {
                                IsPlaying = true;
                                if (Playing != null)
                                    Playing(this, new PlaybackUriEventArgs(operation.Uri));
                            }
                            else
                            {
                                IsPlaying = false;
                                if (Stopped != null)
                                    Stopped(this, new PlaybackStoppedEventArgs(e));
                            }
                            break;
                        case PlayerOperaion.PlayerAction.STOP:
                            player.Stop();
                            break;
                    }
                }

                if (operation != null && operation.Action == PlayerOperaion.PlayerAction.HALT)
                    break;
            }
            halting = true;
            player.Dispose();
        }

        public void Play(string uri)
        {
            if (Loading != null)
                Loading(this, new PlaybackUriEventArgs(uri));
            operations.Push(new PlayerOperaion(PlayerOperaion.PlayerAction.PLAY, uri));
            waitHandle.Set();
        }

        public void Stop()
        {
            operations.Push(new PlayerOperaion(PlayerOperaion.PlayerAction.STOP));
            waitHandle.Set();
        }

        public void Halt()
        {
            operations.Push(new PlayerOperaion(PlayerOperaion.PlayerAction.HALT));
            waitHandle.Set();
            if (!thread.Join(TimeSpan.FromMilliseconds(200)))
                thread.Abort();
        }

        public bool IsPlaying { get; private set; }

        public event EventHandler<PlaybackUriEventArgs> Loading;
        public event EventHandler<PlaybackUriEventArgs> Playing;
        public event EventHandler<PlaybackStoppedEventArgs> Stopped;
    }
}
