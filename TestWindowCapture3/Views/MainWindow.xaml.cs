using Livet;
using Lvhang.WindowsCapture;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media.Imaging;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;

namespace TestWindowCapture3.Views
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

		private GraphicsCaptureItem _item;
		private IDirect3DDevice _device;
		private GraphicsCaptureSession _session;
		private Direct3D11CaptureFramePool _framePool;
		private async void StartButton_Click(object sender, RoutedEventArgs e)
		{
			try {
#if false
				// 1. ピッカーのインスタンス化
				var picker = new GraphicsCapturePicker();
				// 2. WPFのウィンドウハンドル(HWND)を取得してピッカーに紐付け
				var windowHandle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
				WinRT.Interop.InitializeWithWindow.Initialize(picker, windowHandle);
				// 3. ユーザーにキャプチャ対象を選択させる
				_item = await picker.PickSingleItemAsync();
#else
				// 1.ウィンドウハンドルを取得 
				nint hWnd = 0;
				foreach (var proc in Process.GetProcesses()) {
					if (proc.MainWindowTitle.Contains("Edge")) {
						hWnd = proc.MainWindowHandle;
						break;
					}
				}
				// 2.ウィンドウハンドルから GraphicsCaptureItem に変換
				_item = TestWindowCapture.CsWin32.GraphicsCaptureHelper.CreateForWindow(hWnd);
#endif
				if (_item == null) {
					return;
				}
				// 4.ウィンドウが閉じた時のイベントハンドラ
				_item.Closed += GraphicsCaptureItem_Closed;
				// 5. グラフィックデバイスの準備
				_device = Direct3D11Helper.CreateDevice();
				// 6. フレームプールの作成
				_framePool = Direct3D11CaptureFramePool.Create(_device, DirectXPixelFormat.B8G8R8A8UIntNormalized, 2, _item.Size);
				// 7. フレームが到着したときのイベントハンドラ
				_framePool.FrameArrived += (senderPool, args) => {
					using Direct3D11CaptureFrame frame = senderPool.TryGetNextFrame();
					if (frame != null) {
						DispatcherHelper.UIDispatcher.Invoke(() => {
							BitmapImage image = new();
							image.BeginInit();
							image.CacheOption = BitmapCacheOption.OnLoad;
							image.StreamSource = frame.ToBitmapStream();
							image.EndInit();
							image.Freeze();
							CaptureImage.Source = image;
						});
					}
				};
				// 8. セッションの開始
				_session = _framePool.CreateCaptureSession(_item);
				_session.IsBorderRequired = false;
				_session.IsCursorCaptureEnabled = false;
				_session.StartCapture();

				StartButton.IsEnabled = false;
				StopButton.IsEnabled = true;
			} catch (Exception ex) {
				MessageBox.Show($"エラーが発生しました: {ex.Message}");
			}
		}
		private void GraphicsCaptureItem_Closed(GraphicsCaptureItem sender, object args)
		{
			Application.Current.Dispatcher.Invoke(() => {
				StopButton_Click(null, null);
			});
		}

		private void StopButton_Click(object sender, RoutedEventArgs e)
		{
			StartButton.IsEnabled = true;
			StopButton.IsEnabled = false;

			_session?.Dispose();
			_framePool?.Dispose();
			_device?.Dispose();
			_session = null;
			_framePool = null;
			_device = null;
			_item = null;
		}
	}
}
