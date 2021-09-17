using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using Windows.Media.Control;
using Windows.Storage.Streams;

namespace GlobalMediaControl
{
    /// <summary>
    /// Interaction logic for GlobalMediaControl.xaml
    /// </summary>
    public partial class MediaControlBar : UserControl
    {
        private GlobalSystemMediaTransportControlsSessionManager SMTCManager;
        private GlobalSystemMediaTransportControlsSession SMTCSession { get; set; }
        public GlobalSystemMediaTransportControlsSessionMediaProperties MediaProps { get; internal set; }
        public GlobalSystemMediaTransportControlsSessionPlaybackInfo PlayBackInfo { get; internal set; }
        public ImageSource AlbumImgSrc { get; internal set; }

        public MediaControlBar()
        {
            InitializeComponent();
            InitMediaSessionManager();
        }

        private void UpdateUI(Action act)
        {
            Dispatcher.Invoke(act);
            //Application.Current.Dispatcher.Invoke(act); // in COM dll, we can't find application, use Dispatcher instead
        }

        private async void InitMediaSessionManager()
        {
            SMTCManager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            if (SMTCManager == null)
            {
                return;
            }
            SMTCManager.CurrentSessionChanged += CurrentSessionChanged;
            CurrentSessionChanged(SMTCManager, null);
        }

        private void CurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager sender, CurrentSessionChangedEventArgs args)
        {
            if (sender == null)
            {
                return;
            }
            SMTCSession = sender.GetCurrentSession();
            if (SMTCSession != null)
            {
                SMTCSession.MediaPropertiesChanged += MediaPropertiesChanged;
                SMTCSession.PlaybackInfoChanged += PlaybackInfoChanged;
                MediaPropertiesChanged(SMTCSession, null);
                PlaybackInfoChanged(SMTCSession, null);
            }
        }

        private void PlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
        {
            if (sender == null)
            {
                return;
            }
            PlayBackInfo = sender.GetPlaybackInfo();
            if (PlayBackInfo != null)
            {
                UpdateUI(() =>
                {
                    bool playing = PlayBackInfo.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
                    playImg.Source = playing ? pauseIcon.Source : playIcon.Source;
                });
            }
        }

        private async void MediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
        {
            if (sender == null)
            {
                return;
            }
            MediaProps = await sender.TryGetMediaPropertiesAsync();

            if (MediaProps != null)
            {
                if (MediaProps.Thumbnail != null)
                {
                    IRandomAccessStreamWithContentType stream = await MediaProps.Thumbnail.OpenReadAsync();
                    var decoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(stream);
                    var data = (await decoder.GetPixelDataAsync()).DetachPixelData();
                    UpdateUI(() =>
                    {
                        try
                        {
                            AlbumImgSrc = System.Windows.Media.Imaging.BitmapSource.Create(
                                (int)decoder.PixelWidth, (int)decoder.PixelHeight,
                                decoder.DpiX, decoder.DpiY,
                                PixelFormats.Bgra32,
                                null,
                                data,
                                (int)decoder.PixelWidth * 4
                                );
                        }
                        catch
                        {
                            AlbumImgSrc = albumIcon.Source;
                        }
                    });
                }
                else
                {
                    UpdateUI(() => { AlbumImgSrc = albumIcon.Source; });
                }

                UpdateUI(() =>
                {
                    albumImg.Source = AlbumImgSrc;
                    titleLable.Content = MediaProps.Title;
                    artistLable.Content = MediaProps.Artist + " - " + MediaProps.AlbumTitle;
                });
            }
        }
        private void ToggleControls()
        {
            if (textGrid.Visibility == Visibility.Visible)
            {
                textGrid.Visibility = Visibility.Hidden;
                actionGrid.Visibility = Visibility.Visible;
            }
            else
            {
                textGrid.Visibility = Visibility.Visible;
                actionGrid.Visibility = Visibility.Hidden;
            }
        }

        private void SkipNowPlaying(bool next)
        {
            if (next)
                OnNextBtnClick(null, null);
            else
                OnPrevBtnClick(null, null);
        }

        private async void OnPrevBtnClick(object sender, RoutedEventArgs e)
        {
            if (SMTCSession == null)
            {
                return;
            }
            await SMTCSession.TrySkipPreviousAsync();
        }

        private async void OnNextBtnClick(object sender, RoutedEventArgs e)
        {
            if (SMTCSession == null)
            {
                return;
            }

            await SMTCSession.TrySkipNextAsync();
        }

        private async void OnPlayBtnClick(object sender, RoutedEventArgs e)
        {
            if (SMTCSession == null)
            {
                return;
            }
            await SMTCSession.TryTogglePlayPauseAsync();
        }

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            SkipNowPlaying(e.Delta > 0);
        }

        private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.ClickCount == 2)
            {
                ToggleControls();
            } else if (e.ChangedButton == MouseButton.Middle) {
                OnPlayBtnClick(sender, new RoutedEventArgs());
            }
        }
    }
}
