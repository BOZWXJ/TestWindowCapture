using Lvhang.WindowsCapture;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;
using Windows.Win32.UI.WindowsAndMessaging;

namespace TestWindowCapture.CsWin32
{
	public class WindowCapture
	{
		public enum WindowRectType { Window, ExFrame, Client }

		/// <summary>
		/// 指定した文字列が含まれるウィンドウハンドルを返す
		/// </summary>
		/// <param name="title"></param>
		/// <returns></returns>
		public static nint GetWindow(string title)
		{
			foreach (var proc in Process.GetProcesses()) {
				if (proc.MainWindowTitle.Contains(title)) {
					return proc.MainWindowHandle;
				}
			}
			return 0;
		}

		/// <summary>
		/// 指定した座標のウィンドウハンドルを返す
		/// </summary>
		/// <param name="pos"></param>
		/// <returns></returns>
		public static nint GetWindow(System.Drawing.Point pos)
		{
			// ウィンドウハンドル
			HWND hWnd;
			if (0 != (hWnd = PInvoke.WindowFromPoint(pos))) {
				// 親ウィンドウ
				return PInvoke.GetAncestor(hWnd, GET_ANCESTOR_FLAGS.GA_ROOT);
			}
			return 0;
		}

		/// <summary>
		/// ウィンドウハンドルの Rect を返す
		/// </summary>
		/// <param name="hWnd"></param>
		/// <param name="rectType"></param>
		/// <returns></returns>
		/// <exception cref="Win32Exception"></exception>
		public static unsafe Rectangle GetWindowRect(nint hWnd, WindowRectType rectType)
		{
			Rectangle rectangle = Rectangle.Empty;
			switch (rectType) {
			case WindowRectType.Window:
				// 1.GetWindowRect ドロップシャドウ等を含む
				if (!PInvoke.GetWindowRect((HWND)hWnd, out RECT wRect)) {
					throw new Win32Exception();
				}
				rectangle = wRect;
				break;
			case WindowRectType.ExFrame:
				// 2.DwmGetWindowAttribute ドロップシャドウ等は含まない？
				RECT exRect = default;
				HRESULT hr = PInvoke.DwmGetWindowAttribute((HWND)hWnd, DWMWINDOWATTRIBUTE.DWMWA_EXTENDED_FRAME_BOUNDS, &exRect, (uint)sizeof(RECT));
				if (hr.Failed) {
					Marshal.ThrowExceptionForHR(hr);
				}
				rectangle = exRect;
				break;
			case WindowRectType.Client:
				// 3.GetClientRect クライアント領域
				if (!PInvoke.GetClientRect((HWND)hWnd, out RECT cRect)) {
					throw new Win32Exception();
				}
				var pos = new System.Drawing.Point();
				if (!PInvoke.ClientToScreen((HWND)hWnd, ref pos)) {
					throw new Win32Exception();
				}
				rectangle = new(pos.X, pos.Y, cRect.Width, cRect.Height);
				break;
			}
			return rectangle;
		}

		/// <summary>
		/// ウィンドウを最前面
		/// </summary>
		/// <param name="hWnd"></param>
		/// <exception cref="Win32Exception"></exception>
		public static void WindowToTop(nint hWnd)
		{
			if (!PInvoke.BringWindowToTop((HWND)hWnd)) {
				throw new Win32Exception();
			}
		}

		/// <summary>
		/// 指定した位置のコントロールを返す
		/// </summary>
		/// <param name="hWnd"></param>
		/// <param name="pos"></param>
		/// <param name="hCnt"></param>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException"></exception>
		/// <exception cref="Win32Exception"></exception>
		public static Rectangle GetChildWindow(nint hWnd, System.Drawing.Point pos, out nint hCnt)
		{
			if (0 == (hCnt = PInvoke.ChildWindowFromPointEx((HWND)hWnd, pos, CWP_FLAGS.CWP_ALL))) {
				throw new InvalidOperationException();
			}
			if (!PInvoke.GetClientRect((HWND)hCnt, out RECT cRect)) {
				throw new Win32Exception();
			}
			var p = new System.Drawing.Point();
			if (!PInvoke.ClientToScreen((HWND)hCnt, ref p)) {
				throw new Win32Exception();
			}
			return new(p.X, p.Y, cRect.Width, cRect.Height);
		}

		/// <summary>
		/// キャプチャ中ウィンドウハンドル
		/// </summary>
		public nint TargetWindowHandle { get; private set; }
		private GraphicsCaptureItem? _item;
		private IDirect3DDevice? _device;
		private Direct3D11CaptureFramePool? _framePool;
		private GraphicsCaptureSession? _session;
		private Action<Bitmap>? FrameArrivedCallback;
		/// <summary>
		/// Windows.Graphics.Capture 開始
		/// </summary>
		/// <param name="hWnd"></param>
		/// <param name="frameArrivedCallback"></param>
		public void Start(nint hWnd, Action<Bitmap> frameArrivedCallback)
		{
			Application.Current.Dispatcher.Invoke(() => {
				try {
					TargetWindowHandle = hWnd;
					FrameArrivedCallback = frameArrivedCallback;
					// 1. ウィンドウハンドルから GraphicsCaptureItem 取得
					_item = GraphicsCaptureHelper.CreateForWindow(hWnd);
					// 2. ウィンドウが閉じた時のイベントハンドラ
					_item.Closed += OnCaptureTargetClosed;
					// 3. グラフィックデバイスの準備
					_device = Direct3D11Helper.CreateDevice();
					// 4. フレームプールの作成
					_framePool = Direct3D11CaptureFramePool.Create(_device, DirectXPixelFormat.B8G8R8A8UIntNormalized, 2, _item.Size);
					// 5. フレームが到着したときのイベントハンドラ
					_framePool.FrameArrived += (sender, args) => {
						using Direct3D11CaptureFrame frame = sender.TryGetNextFrame();
						if (frame != null && FrameArrivedCallback != null) {
							FrameArrivedCallback(frame.ToBitmap());
						}
					};
					// 6. セッションの開始
					_session = _framePool.CreateCaptureSession(_item);
					_session.IsBorderRequired = false;          // キャプチャ対象ウィンドウに黄色い枠を表示しない
					_session.IsCursorCaptureEnabled = false;    // キャプチャにカーソルを含めない
					_session.StartCapture();
				} catch {
					Stop();
					throw;
				}
			});
		}
		private void OnCaptureTargetClosed(GraphicsCaptureItem sender, object args)
		{
			Application.Current.Dispatcher.Invoke(() => {
				sender.Closed -= OnCaptureTargetClosed;
				Stop();
				Closed?.Invoke(null, new());
			});
		}

		/// <summary>
		/// Windows.Graphics.Capture ターゲットウィンドウが閉じた
		/// </summary>
		public event EventHandler<object>? Closed;

		/// <summary>
		/// Windows.Graphics.Capture 終了
		/// </summary>
		public void Stop()
		{
			Application.Current.Dispatcher.Invoke(() => {
				_session?.Dispose();
				_framePool?.Dispose();
				_device?.Dispose();
				_session = null;
				_framePool = null;
				_device = null;
				_item = null;
				TargetWindowHandle = 0;
			});
		}
	}
}
