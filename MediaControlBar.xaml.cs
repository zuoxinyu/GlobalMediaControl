using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using System.Globalization;

using Windows.Media.Control;
using Windows.Storage.Streams;

namespace GlobalMediaControl
{
    /// <summary>
    /// Interaction logic for GlobalMediaControl.xaml
    /// </summary>
    public partial class MediaControlBar : UserControl
    {
        public static readonly ImageSource DefaultAlbumImgSrc = new BitmapImage(new Uri("pack://application:,,,/GlobalMediaControl;component/Resources/album-32.jpg"));

        private Storyboard SlideUp, SlideDown, Fade, Marquee;

        private DoubleAnimation TitleAnime;

        private bool LastSkipActionIsPrev = false;

        private GlobalSystemMediaTransportControlsSessionManager SMTCManager;
        private GlobalSystemMediaTransportControlsSession SMTCSession { get; set; }
        public GlobalSystemMediaTransportControlsSessionMediaProperties MediaProps { get; internal set; }
        public GlobalSystemMediaTransportControlsSessionPlaybackInfo PlayBackInfo { get; internal set; }
        public ImageSource AlbumImgSrc { get; internal set; }
        public string ArtistLine
        {
            get => MediaProps == null ? null : MediaProps.Artist + " - " + MediaProps.AlbumTitle;
            internal set { }
        }
        public double DesiredTextSize { get; internal set; }

        public MediaControlBar()
        {
            InitializeComponent();
            InitMediaSessionManager();

            AlbumImgSrc = DefaultAlbumImgSrc;

            TitleAnime = new DoubleAnimation();
            TitleAnime.From = 0;
            TitleAnime.To = 0;
            TitleAnime.Duration = new Duration(TimeSpan.FromSeconds(3));
            TitleAnime.RepeatBehavior = new RepeatBehavior(1);
            TitleAnime.FillBehavior = FillBehavior.Stop;

            Fade = (Storyboard)Resources["fadeStoryBoard"];
            Marquee = (Storyboard)Resources["marqueeStoryBoard"];
            SlideUp = (Storyboard)Resources["slideUpStoryBoard"];
            SlideDown = (Storyboard)Resources["slideDownStoryBoard"];

            SlideUp.Completed += (s, e) => LastSkipActionIsPrev = false;
            SlideDown.Completed += (s, e) => LastSkipActionIsPrev = false;
            // SlideUp.Completed += (s, e) => OnTitleLableSizeChanged(null, null);
            // SlideDown.Completed += (s, e) => OnTitleLableSizeChanged(null, null);
            // canvas.SizeChanged += OnTitleLableSizeChanged;

        }

        private void UpdateUI(Action act)
        {
            Dispatcher.Invoke(act);
        }

        private async void InitMediaSessionManager()
        {
            SMTCManager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            if (SMTCManager == null)
            {
                return;
            }
            SMTCManager.CurrentSessionChanged += OnCurrentSessionChanged;
            OnCurrentSessionChanged(SMTCManager, null);
        }

        private void OnCurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager sender, CurrentSessionChangedEventArgs args)
        {
            if (sender == null)
            {
                return;
            }
            SMTCSession = sender.GetCurrentSession();
            if (SMTCSession != null)
            {
                SMTCSession.MediaPropertiesChanged += OnMediaPropertiesChanged;
                SMTCSession.PlaybackInfoChanged += OnPlaybackInfoChanged;
                OnMediaPropertiesChanged(SMTCSession, null);
                OnPlaybackInfoChanged(SMTCSession, null);
            }
        }

        private void OnPlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
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

        private async void OnMediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
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
                            AlbumImgSrc = DefaultAlbumImgSrc;
                        }
                    });
                }
                else
                {
                    UpdateUI(() => { AlbumImgSrc = DefaultAlbumImgSrc; });
                }

                UpdateUI(() =>
                {
                    albumImg.Source = AlbumImgSrc;
                    titleBlock.Content = MediaProps.Title;
                    artistBlock.Text = ArtistLine;
                    if (LastSkipActionIsPrev)
                    {
                        SlideDown.Begin(textGrid);
                    }
                    else
                    {
                        SlideUp.Begin(textGrid);
                    }
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
            Fade.Begin(contentGrid);
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
            LastSkipActionIsPrev = await SMTCSession.TrySkipPreviousAsync();
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
            }
            else if (e.ChangedButton == MouseButton.Middle)
            {
                OnPlayBtnClick(sender, new RoutedEventArgs());
            }
        }

        private void OnTitleLableSizeChanged(object sender, SizeChangedEventArgs e)
        {
            double offset = titleBlock.DesiredSize.Width - canvas.RenderSize.Width;

            if (offset <= 0)
            {
                return;
            }
            var story = new Storyboard();
            var anime = TitleAnime.Clone();
            anime.To = -offset;
            story.Children.Add(anime);
            Storyboard.SetTargetName(anime, "marquee");
            Storyboard.SetTargetProperty(anime, new PropertyPath(TranslateTransform.XProperty));
            story.Begin(titleBlock);
        }

        private Size MeasureString(string candidate)
        {
             var typeFace = new Typeface(
                 titleBlock.FontFamily,
                 titleBlock.FontStyle,
                 titleBlock.FontWeight,
                 titleBlock.FontStretch);

            var culture = new CultureInfo("zh-CN");
            var formattedText = new FormattedText(
                candidate,
                culture,
                FlowDirection.LeftToRight,
                typeFace,
                titleBlock.FontSize,
                Brushes.Black,
                new NumberSubstitution(),
                1);

            return new Size(formattedText.Width, formattedText.Height);
        }
    }
}
