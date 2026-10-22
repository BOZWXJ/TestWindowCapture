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
			if (!CaptureRunning) {
				return;
			}
			// ウィンドウを最前面
			WindowCapture.WindowToTop(_windowCapture.TargetWindowHandle);
			// ウィンドウ位置サイズ
			Rectangle exRect = WindowCapture.GetWindowRect(_windowCapture.TargetWindowHandle, WindowCapture.WindowRectType.ExFrame);
			System.Diagnostics.Debug.WriteLine($"window {_windowCapture.TargetWindowHandle:x}");
			System.Diagnostics.Debug.WriteLine($"       {exRect}");
			// ドキュメント領域
			Rectangle rect = WindowCapture.GetChildWindow(_windowCapture.TargetWindowHandle, new(exRect.Width / 2, exRect.Height / 2), out nint hCnt);
			System.Diagnostics.Debug.WriteLine($"client {hCnt:x}");
			System.Diagnostics.Debug.WriteLine($"       {rect}");
			// キャプチャ取得
			var tmp = Volatile.Read(ref bitmap);
			if (tmp == null) {
				return;
			}
			System.Diagnostics.Debug.WriteLine($"bitmap {tmp.Size}");
			// 切り抜き
			rect.Offset(-exRect.X, -exRect.Y);
			CapturedBitMap = tmp.Clone(rect, tmp.PixelFormat);
		}

		public void Stop()
		{
			_windowCapture.Stop();
			CaptureRunning = false;
		}
	}
}
