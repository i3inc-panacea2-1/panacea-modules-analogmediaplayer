using Panacea.Core;
using Panacea.Modularity;
using Panacea.Modularity.Media;
using Panacea.Modularity.Media.Channels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using VisioForge.Controls.UI.WinForms;
using VisioForge.Types;
using VisioForge.Types.VideoEffects;
using Color = System.Drawing.Color;

namespace Panacea.Modules.AnalogMediaPlayer
{
    /// <summary>
    /// Interaction logic for VisioForgeMediaPlayerWPF.xaml
    /// </summary>
    public partial class VisioForgeMediaPlayer : UserControl, IMediaPlayerPlugin
    {
        private List<Type> _supportedTypes = new List<Type>() { typeof(AnalogMedia) };
        private VideoCapture visioforgePlayer;
        private readonly ILogger _logger;
        //private readonly AudioManager _audioManager;

        public VisioForgeMediaPlayer(PanaceaServices core /*, AudioManager audioManager*/)
        {
            _logger = core.Logger;
            //_audioManager = audioManager;
            InitializeComponent();
            try
            {
                visioforgePlayer = new VideoCapture
                {
                    Dock = System.Windows.Forms.DockStyle.Fill,
                    Mode = VFVideoCaptureMode.VideoPreview,
                    Debug_Mode = false
                };
                visioforgePlayer.SetLicenseKey("1DCD-7E69-5B15-9FBC-2200-4340", "", "");
                visioforgePlayer.OnError += VisioforgePlayer_OnError;
                //visioforgePlayer.Debug_Dir = Utils.Path();
                //Content = visioforgePlayer;
                visioforgePlayer.Click += (oo, ee) => OnClick();
                PanelBox.Child = (visioforgePlayer);
            }
            catch (Exception ex)
            {
                _logger.Error(this, "Exception during Visioforge initialization / " + ex.Message);
            }
        }

        Task IPlugin.BeginInit()
        {
            return Task.CompletedTask;
        }

        public bool HasMoreChapters()
        {
            return false;
        }

        Task IPlugin.EndInit()
        {
            return Task.CompletedTask;
        }

        public Task Shutdown()
        {
            return Task.CompletedTask;
        }

        public bool HasSubtitles => false;
        

        private void VisioforgePlayer_OnError(object sender, ErrorsEventArgs e)
        {
            //RaiseEvent(() => OnError(new Exception(e.Message)));
        }

        public bool IsSeekable
        {
            get { return false; }
        }

        public bool CanPlayChannel(object item)
        {
            return item is AnalogMedia;
        }

        public async void Play(MediaItem channel)
        {
            try
            {
                if (visioforgePlayer.Status == VFVideoCaptureStatus.Work) visioforgePlayer.Stop();
                else
                {
                    visioforgePlayer.RefreshLists(true, false, false);
                }
                OnOpening();
                visioforgePlayer.Mode = VFVideoCaptureMode.VideoPreview;
                visioforgePlayer.Audio_OutputDevice = "Default WaveOut Device";
                visioforgePlayer.Video_ResizeOrCrop_Enabled = true;
                visioforgePlayer.Audio_CaptureDevice = string.Empty;
                visioforgePlayer.Video_CaptureDevice_IsAudioSource = true;
                visioforgePlayer.Audio_PlayAudio = true;
                visioforgePlayer.TVTuner_Mode = VFTVTunerMode.TV;

                visioforgePlayer.Video_Renderer.Video_Renderer = VFVideoRenderer.EVR;

                visioforgePlayer.Video_Effects_Clear();
                visioforgePlayer.Audio_Effects_Enabled = false;
                visioforgePlayer.BackColor = Color.Black;
                visioforgePlayer.Video_Renderer.StretchMode = VFVideoRendererStretchMode.Stretch;
                
                    visioforgePlayer.Video_Effects_Enabled = true;
                    IVFVideoEffectDeinterlaceCAVT cavt;
                    var effect = visioforgePlayer.Video_Effects_Get("DeinterlaceCAVT");
                    if (effect == null)
                    {
                        cavt = new VFVideoEffectDeinterlaceCAVT(true, 127);
                        visioforgePlayer.Video_Effects_Add(cavt);
                    }
                
                visioforgePlayer.Video_Resize = new VideoResizeSettings()
                {
                    Mode = VFResizeMode.Bicubic
                };
                visioforgePlayer.DV_Decoder_Video_Resolution = VFDVVideoResolution.Full;
                visioforgePlayer.TVTuner_Name = visioforgePlayer.TVTuner_Devices[0];
                var captureDevice =
                    visioforgePlayer.Video_CaptureDevicesInfo.First(d => !string.IsNullOrEmpty(d.TVTuner)).Name;
                visioforgePlayer.Video_CaptureDevice = captureDevice;
                visioforgePlayer.Video_CaptureDevice_IsAudioSource = true;
                visioforgePlayer.Audio_PlayAudio = true;
                System.Windows.Forms.Application.DoEvents();
                //visioforgePlayer.Video_Renderer_Stretch = true;
                visioforgePlayer.TVTuner_InputType = (channel as AnalogMedia).Source;
                switch ((channel as AnalogMedia).CountryCode)
                {
                    case "1":
                        visioforgePlayer.TVTuner_TVFormat = VFTVTunerVideoFormat.NTSC_M;
                        //visioforgePlayer.Video_CaptureDevice_Format = VFDVVideoFormat.NTSC;
                        visioforgePlayer.TVTuner_Country = "Canada";
                        break;
                    default:
                        visioforgePlayer.TVTuner_TVFormat = VFTVTunerVideoFormat.PAL_B;
                        //visioforgePlayer.DV_Capture_Video_Format = VFDVVideoFormat.PAL;
                        visioforgePlayer.TVTuner_Country = "Greece";
                        break;
                }
                var number = (channel as AnalogMedia).ChannelNumber;
                if (number > 999)
                {
                    visioforgePlayer.TVTuner_Frequency = number;

                    visioforgePlayer.TVTuner_Channel = 0;
                }
                else
                {
                    visioforgePlayer.TVTuner_Channel = number;
                }

                OnOpening();

                //var volume = _audioManager.SpeakersVolume;

              

                visioforgePlayer.TVTuner_Apply();

              

                visioforgePlayer.TVTuner_Read();

               

                IsPlaying = true;
                OnPlaying();
                OnNowPlaying(channel.Name);
                await Dispatcher.BeginInvoke(new Action(() =>
                {
                    visioforgePlayer.Start();

                    _logger.Debug(this, "step 4: " + visioforgePlayer.TVTuner_Channel);

                    //They had to do one thing. They couldn't. We have to do 40 things using their things...
                    //Do not remove this line
                    //_audioManager.SpeakersVolume = volume;

                    visioforgePlayer.TVTuner_Channel = number;
                }));

            }
            catch (Exception ex)
            {
                OnError(ex);
            }
        }

        public void Stop()
        {
            try
            {
                IsPlaying = false;
                visioforgePlayer.Stop();
                OnStopped();
            }
            catch
            {
            }
        }

        public void Pause()
        {
            IsPlaying = false;
            visioforgePlayer.Pause();
        }

        public void Seek(int percentage)
        {

        }

        public bool IsPlaying { get; protected set; }

        public TimeSpan Duration
        {
            get { return new TimeSpan(0); }
        }

        public bool IsPausable
        {
            get { return true; }
        }

        public void Play()
        {
            try
            {
                IsPlaying = true;
                visioforgePlayer.Start();
            }
            catch { }
        }

        public void Dispose()
        {
            visioforgePlayer.Dispose();
        }

        public float Position
        {
            get { return 0; }
            set { }
        }

        public bool HasNext
        {
            get { return false; }
        }

        public bool HasPrevious
        {
            get { return false; }
        }

        public void Next()
        {

        }

        public void Previous()
        {

        }

        public void NextSubtitle()
        {

        }

        public void SetSubtitles(bool val)
        {
            
        }
        public event EventHandler<bool> IsSeekableChanged;
        public event EventHandler<bool> HasNextChanged;
        public event EventHandler<bool> HasPreviousChanged;
        public event EventHandler IsPausableChanged;

        public event EventHandler Stopped;
        protected void OnStopped()
        {
            Stopped?.Invoke(this, null);
        }

        public event EventHandler<Exception> Error;
        protected void OnError(Exception ex)
        {
            Error?.Invoke(this, ex);
        }

        public event EventHandler Playing;
        protected void OnPlaying()
        {
            Playing?.Invoke(this, null);
        }

        public event EventHandler<string> NowPlaying;
        protected void OnNowPlaying(string str)
        {
            NowPlaying?.Invoke(this, str);
        }

        public event EventHandler Opening;
        protected void OnOpening()
        {
            Opening?.Invoke(this, null);
        }

        public event EventHandler Click;
        protected void OnClick()
        {
            Click?.Invoke(this, null);
        }

        public event EventHandler<float> PositionChanged;
        protected void OnPositionChanged(float e)
        {
            PositionChanged?.Invoke(this, e);
        }

        public event EventHandler<TimeSpan> DurationChanged;
        protected void OnDurationChanged(TimeSpan duration)
        {
            
            DurationChanged?.Invoke(this, duration);
        }
        

        public FrameworkElement VideoControl => this;
        public event EventHandler<bool> HasSubtitlesChanged;
        protected void OnHasSubtitlesChanged(bool val)
        {
            HasSubtitlesChanged?.Invoke(this, val);
        }

        public event EventHandler Paused;
        protected void OnPaused()
        {
            Paused?.Invoke(this, null);
        }

        public event EventHandler Ended;
        protected void OnEnded()
        {
            Ended?.Invoke(this, null);
        }
    }
}

