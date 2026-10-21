using Lvhang.WindowsCapture;
using System;
using System.Windows;
using System.Windows.Media.Imaging;

namespace TestWindowCapture2.Views
{
	/* 
     * If some events were receive from ViewModel, then please use PropertyChangedWeakEventListener and CollectionChangedWeakEventListener.
     * If you want to subscribe custome events, then you can use LivetWeakEventListener.
     * When window closing and any timing, Dispose method of LivetCompositeDisposable is useful to release subscribing events.
     *
     * Those events are managed using WeakEventListener, so it is not occurred memory leak, but you should release explicitly.
     */
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
		}

		WindowsCaptureSession _captureSession;
		private void StartButton_Click(object sender, RoutedEventArgs e)
		{
			StartButton.IsEnabled = false;
			StopButton.IsEnabled = true;

			// Lvhang.WindowsCapture 使用
			_captureSession = new WindowsCaptureSession(this, new WindowsCaptureSessionOptions() {
				//set minial ms between frames
				MinFrameInterval = 50,
				IsManual = true,
			});
			_captureSession.OnFrameArrived += CaptureSession_OnFrameArrived;

			_captureSession.PickAndCapture();
		}
		private void StopButton_Click(object sender, RoutedEventArgs e)
		{
			StartButton.IsEnabled = true;
			StopButton.IsEnabled = false;

			_captureSession.StopCapture();
		}

		private async void CaptureSession_OnFrameArrived(Windows.Graphics.Capture.Direct3D11CaptureFrame frame, Action nextFrame)
		{
			BitmapImage image = new();
			image.BeginInit();
			image.CacheOption = BitmapCacheOption.OnLoad;
			image.StreamSource = frame.ToBitmapStream();
			image.EndInit();
			image.Freeze();
			CaptureImage.Source = image;
			//enable to get next frame, if set IsManual to false, you don't have to call this method to get next frame.
			nextFrame();
		}
	}
}
