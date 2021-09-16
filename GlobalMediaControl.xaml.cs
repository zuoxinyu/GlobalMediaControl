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
    public partial class GolobalMediaControl : UserControl
    {
        private GlobalSystemMediaTransportControlsSessionManager mgr_;
        private GlobalSystemMediaTransportControlsSession sess_;
        private GlobalSystemMediaTransportControlsSessionMediaProperties props_;
        private GlobalSystemMediaTransportControlsSessionPlaybackInfo info_;
        private ImageSource bitmapSource;

        public GolobalMediaControl()
        {
            InitializeComponent();
            initMediaSessionManager();
        }

        private void updateUI(Action act)
        {
            Dispatcher.Invoke(act);
            //Application.Current.Dispatcher.Invoke(act); // in COM dll, we can't find application, use Dispatcher instead
        }

        private async void initMediaSessionManager()
        {
            mgr_ = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            if (mgr_ == null)
            {
                return;
            }
            mgr_.CurrentSessionChanged += CurrentSessionChanged;
            CurrentSessionChanged(mgr_, null);
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            File.AppendAllText("D:\\out.txt", e.ExceptionObject.GetType().FullName + "\n" + e.ExceptionObject.ToString() + "\n");
        }

        private void CurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager sender, CurrentSessionChangedEventArgs args)
        {
            if (sender == null)
            {
                return;
            }
            sess_ = sender.GetCurrentSession();
            if (sess_ != null)
            {
                sess_.MediaPropertiesChanged += MediaPropertiesChanged;
                sess_.PlaybackInfoChanged += PlaybackInfoChanged;
                MediaPropertiesChanged(sess_, null);
                PlaybackInfoChanged(sess_, null);
            }
        }

        private void PlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
        {
            if (sender == null)
            {
                return;
            }
            info_ = sender.GetPlaybackInfo();
            if (info_ != null)
            {
                updateUI(() =>
                {
                    bool playing = info_.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
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
            props_ = await sender.TryGetMediaPropertiesAsync();

            if (props_ != null)
            {
                if (props_.Thumbnail != null)
                {
                    IRandomAccessStreamWithContentType stream = await props_.Thumbnail.OpenReadAsync();
                    var decoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(stream);
                    var data = (await decoder.GetPixelDataAsync()).DetachPixelData();
                    updateUI(() =>
                    {
                        try
                        {
                            bitmapSource = System.Windows.Media.Imaging.BitmapSource.Create(
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
                            bitmapSource = albumIcon.Source;
                        }
                    });
                }
                else
                {
                    updateUI(() => { bitmapSource = albumIcon.Source; });
                }

                updateUI(() =>
                {
                    albumImg.Source = bitmapSource;
                    titleLable.Content = props_.Title;
                    artistLable.Content = props_.Artist + " - " + props_.AlbumTitle;
                });
            }
        }
        private void toggleControls()
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

        private void skipNowPlaying(bool next)
        {
            if (next)
                nextBtn_Click(null, null);
            else
                prevBtn_Click(null, null);
        }

        private async void prevBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sess_ == null)
            {
                return;
            }
            await sess_.TrySkipPreviousAsync();
        }

        private async void nextBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sess_ == null)
            {
                return;
            }

            await sess_.TrySkipNextAsync();
        }

        private async void playBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sess_ == null)
            {
                return;
            }
            await sess_.TryTogglePlayPauseAsync();
        }

        private void albumImg_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            toggleControls();
        }

        private void Grid_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            skipNowPlaying(e.Delta > 0);
        }
    }
}
