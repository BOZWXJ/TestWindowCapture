using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Windows.Graphics.Capture;

namespace TestWindowCapture.CsWin32
{
	public static partial class GraphicsCaptureHelper
	{
		private static readonly Guid GraphicsCaptureItemIid = new("79C3F95B-31F7-4EC2-A464-632EF5D30760");
		[GeneratedComInterface]
		[Guid("3628E81B-3CAC-4C60-B7F4-23CE0E0C3356")]
		[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		public partial interface IGraphicsCaptureItemInterop
		{
			nint CreateForWindow(nint window, in Guid iid);
			nint CreateForMonitor(nint monitor, in Guid iid);
		}

		public static GraphicsCaptureItem CreateForWindow(nint hwnd)
		{
			if (hwnd == 0) {
				throw new ArgumentException("HWND が無効です。", nameof(hwnd));
			}
			var interop = GraphicsCaptureItem.As<IGraphicsCaptureItemInterop>();
			nint itemPtr = interop.CreateForWindow(hwnd, GraphicsCaptureItemIid);
			if (itemPtr == 0) {
				throw new InvalidOperationException("GraphicsCaptureItem を作成できませんでした。");
			}
			try {
				return GraphicsCaptureItem.FromAbi(itemPtr);
			} finally {
				Marshal.Release(itemPtr);
			}
		}
	}
}
