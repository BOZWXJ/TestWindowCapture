using Livet;
using System;
using System.Drawing;
using System.Threading;
using TestWindowCapture.CsWin32;

namespace TestWindowCapture.Models
{
	public class Model : NotificationObject
	{
		private readonly WindowCapture _windowCapture = new();

		public Model()
		{
			_windowCapture.Closed += WindowCapture_Closed;
		}

		public bool CaptureRunning
		{
			get => _CaptureRunning;
			set => RaisePropertyChangedIfSet(ref _CaptureRunning, value);
		}
		private bool _CaptureRunning;

		public int FrameCount
		{
			get => _FrameCount;
			set => RaisePropertyChangedIfSet(ref _FrameCount, value);
		}
		private int _FrameCount;

		public Bitmap CapturedBitMap
		{
			get => _CapturedBitMap;
			set => RaisePropertyChangedIfSet(ref _CapturedBitMap, value);
		}
		private Bitmap _CapturedBitMap;

		public void Start()
		{
			try {
				// ウィンドウハンドル
				nint hWnd = WindowCapture.GetWindow("Edge");
				if (hWnd == 0) {
					System.Diagnostics.Debug.WriteLine($"ウィンドウが見つかりません");
					CaptureRunning = false;
					return;
				}
				// ウィンドウキャプチャ開始
				_windowCapture.Start(hWnd, Callback);
				CaptureRunning = true;
			} catch (Exception ex) {
				System.Diagnostics.Debug.WriteLine(ex.ToString());
				CaptureRunning = false;
			}
		}

		private void WindowCapture_Closed(object sender, object e)
		{
			CaptureRunning = false;
		}

		private Bitmap bitmap;
		private void Callback(Bitmap bmp)
		{
			FrameCount++;
			Interlocked.Exchange(ref bitmap, bmp);
		}

		public void Capture()
		{
			// キャプチャ取得
			var tmp = Volatile.Read(ref bitmap);
			if (tmp == null) {
				return;
			}
			System.Diagnostics.Debug.WriteLine($"bitmap\t{tmp.Size}");

			nint hWnd = _windowCapture.TargetWindowHandle;
			// ウィンドウ位置サイズ
			Rectangle wRect = WindowCapture.GetWindowRect(hWnd, WindowCapture.WindowRectType.Window);
			System.Diagnostics.Debug.WriteLine($"Window\t{wRect}");
			Rectangle exRect = WindowCapture.GetWindowRect(hWnd, WindowCapture.WindowRectType.ExFrame);
			System.Diagnostics.Debug.WriteLine($"ExFrame\t{exRect}");
			Rectangle cRect = WindowCapture.GetWindowRect(hWnd, WindowCapture.WindowRectType.Client);
			System.Diagnostics.Debug.WriteLine($"Client\t{cRect}");
			// ドキュメント領域
			Rectangle rect = WindowCapture.GetChildWindow(hWnd, new(exRect.Width / 2, exRect.Height / 2), out nint _);
			System.Diagnostics.Debug.WriteLine($"Doc\t{rect}");
			//bitmap  {Width=1111, Height = 788}
			//Window  {X=778, Y = 69, Width = 1125, Height = 795}
			//ExFrame {X=785, Y = 69, Width = 1111, Height = 788}
			//Client  {X=786, Y = 69, Width = 1109, Height = 787}
			//Doc     {X=790, Y = 183, Width = 1101, Height = 669}
			rect.Offset(-exRect.X, -exRect.Y);
			// todo: 切り抜き
			CapturedBitMap = tmp.Clone(rect, tmp.PixelFormat);
		}

		public void Stop()
		{
			_windowCapture.Stop();
			CaptureRunning = false;
		}
	}
}
