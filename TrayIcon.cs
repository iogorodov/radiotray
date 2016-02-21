using CSCore.SoundOut;
using radiotray.Properties;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace radiotray
{
    public class TrayIcon : Form
    {
        private NotifyIcon trayIcon;
        private ContextMenu trayMenu;
        private Timer loadingTimer;
        private int loadingFrame = 0;

        private Icon[] loadingIcons = new Icon[] { Resources.Loading1, Resources.Loading0, Resources.Playing };
        
        private PlayerThread player;
        private string lastPlayed = null;

        public TrayIcon()
        {
            trayMenu = new ContextMenu();
            JsonObject json = SimpleJson.DeserializeObject<JsonObject>(File.ReadAllText("radiotray.json"));
            AppendMenuItems(trayMenu.MenuItems, json);

            trayMenu.MenuItems.Add("-");
            trayMenu.MenuItems.Add("Exit", OnExit);

            trayIcon = new NotifyIcon();
            trayIcon.Text = "Radio Tray";
            trayIcon.Icon = Resources.Idle;

            trayIcon.ContextMenu = trayMenu;
            trayIcon.Visible = true;
            trayIcon.MouseClick += OnIconClick;

            loadingTimer = new Timer();
            loadingTimer.Interval = 200;
            loadingTimer.Tick += OnLoadingTimerTick;

            player = new PlayerThread();

            var onLoading = new EventHandler<PlayerThread.PlaybackUriEventArgs>(OnLoading);
            player.Loading += (object sender, PlayerThread.PlaybackUriEventArgs e) => { Invoke(onLoading, sender, e); };

            var onPlaying = new EventHandler<PlayerThread.PlaybackUriEventArgs>(OnPlaying);
            player.Playing += (object sender, PlayerThread.PlaybackUriEventArgs e) => { Invoke(onPlaying, sender, e); };

            var onStopped = new EventHandler<PlaybackStoppedEventArgs>(OnStopped);
            player.Stopped += (object sender, PlaybackStoppedEventArgs e) => { Invoke(onStopped, sender, e); };
        }

        private void AppendMenuItems(Menu.MenuItemCollection items, JsonObject json)
        {
            foreach (var item in json)
            {
                items.Add(CreateMenuItem(item));
            }
        }

        private MenuItem CreateMenuItem(KeyValuePair<string, object> item)
        {
            if (item.Value is string)
            {
                var menu = new MenuItem(item.Key, OnSelect);
                menu.Tag = item.Value;
                return menu;
            }
            else if (item.Value is JsonObject)
            {
                var menu = new MenuItem(item.Key);
                AppendMenuItems(menu.MenuItems, item.Value as JsonObject);
                return menu;
            }

            return null;
        }

        private void OnLoadingTimerTick(object sender, EventArgs e)
        {
            loadingFrame = (loadingFrame + 1)%loadingIcons.Length;
            trayIcon.Icon = loadingIcons[loadingFrame];
        }

        private void OnExit(object sender, EventArgs e)
        {
            player.Halt();
            Application.Exit();
        }

        private void OnLoading(object sender, PlayerThread.PlaybackUriEventArgs e)
        {
            loadingFrame = 0;
            loadingTimer.Start();
        }

        private void OnPlaying(object sender, PlayerThread.PlaybackUriEventArgs e)
        {
            loadingTimer.Stop();
            trayIcon.Icon = Resources.Playing;
        }

        private void OnStopped(object sender, PlaybackStoppedEventArgs e)
        {
            loadingTimer.Stop();
            if (e.HasError)
            {
                trayIcon.Icon = Resources.Error;
            }
            else
            {
                trayIcon.Icon = Resources.Idle;
            }
        }

        private void OnIconClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;

            if (player.IsPlaying)
            {
                player.Stop();
            }
            else if (lastPlayed != null)
            {
                player.Play(lastPlayed);
            }
        }

        private void OnSelect(object sender, EventArgs e)
        {
            var uri = (string)((MenuItem)sender).Tag;
            lastPlayed = uri;
            player.Play(uri);
        }

        protected override void OnLoad(EventArgs e)
        {
            Visible = false;
            ShowInTaskbar = false;

            base.OnLoad(e);
        }

        protected override void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                trayIcon.Dispose();
            }

            base.Dispose(isDisposing);
        }
    }
}
