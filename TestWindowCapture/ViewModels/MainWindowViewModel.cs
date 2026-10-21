using Livet;
using Livet.Commands;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;
using TestWindowCapture.Models;

namespace TestWindowCapture.ViewModels
{
	public class MainWindowViewModel : ViewModel
	{
		private readonly Model model = new();

		public string FrameCount
		{
			get => _FrameCount;
			set => RaisePropertyChangedIfSet(ref _FrameCount, value);
		}
		private string _FrameCount;

		public BitmapImage CapturedBitMap
		{
			get => _CapturedBitMap;
			set => RaisePropertyChangedIfSet(ref _CapturedBitMap, value);
		}
		private BitmapImage _CapturedBitMap;

		// Some useful code snippets for ViewModel are defined as l*(llcom, llcomn, lvcomm, lsprop, etc...).
		public void Initialize()
		{
			model.PropertyChanged += (sender, e) => {
				DispatcherHelper.UIDispatcher.Invoke(() => {
					if (e.PropertyName == "CaptureRunning") {
						CaptureRunning = model.CaptureRunning;
					} else if (e.PropertyName == "FrameCount") {
						FrameCount = $"{model.FrameCount}";
					} else if (e.PropertyName == "CapturedBitMap") {
						if (model.CapturedBitMap != null) {
							using var ms = new MemoryStream();
							model.CapturedBitMap.Save(ms, ImageFormat.Bmp);
							ms.Position = 0;
							var bitmapImage = new BitmapImage();
							bitmapImage.BeginInit();
							bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
							bitmapImage.CreateOptions = BitmapCreateOptions.None;
							bitmapImage.StreamSource = ms;
							bitmapImage.EndInit();
							bitmapImage.Freeze();
							CapturedBitMap = bitmapImage;
						}
					}
				});
			};
		}

		public bool CaptureRunning
		{
			get => _CaptureRunning;
			set
			{
				if (_CaptureRunning == value) {
					return;
				}
				_CaptureRunning = value;
				StartCommand.RaiseCanExecuteChanged();
				CaptureCommand.RaiseCanExecuteChanged();
				StopCommand.RaiseCanExecuteChanged();
			}
		}
		private bool _CaptureRunning;

		public ListenerCommand<string> StartCommand => _StartCommand ??= new ListenerCommand<string>(Start, CanStart);
		private ListenerCommand<string> _StartCommand;
		public bool CanStart()
		{
			return !CaptureRunning;
		}
		public void Start(string parameter)
		{
			model.Start();
		}

		public ListenerCommand<string> CaptureCommand => _CaptureCommand ??= new ListenerCommand<string>(Capture, CanCapture);
		private ListenerCommand<string> _CaptureCommand;
		private bool CanCapture()
		{
			return CaptureRunning;
		}
		public async void Capture(string parameter)
		{
			model.Capture();
		}

		public ListenerCommand<string> StopCommand => _StopCommand ??= new ListenerCommand<string>(Stop, CanStop);
		private ListenerCommand<string> _StopCommand;
		private bool CanStop()
		{
			return CaptureRunning;
		}
		public void Stop(string parameter)
		{
			model.Stop();
		}
	}
}
