using CyberFeedForward.TheMadArchivist.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace UnitTests.Services;

[TestClass]
public sealed class TrayIconServiceTests
{
    private sealed class FakePopupMenu : TrayIconService.ITrayPopupMenu
    {
        public IntPtr Handle => new(123);

        public System.Collections.Generic.List<(uint id, string text)> Items { get; } = [];

        public void AppendItem(uint commandId, string text)
        {
            Items.Add((commandId, text));
        }

        public void AppendSeparator()
        {
            Items.Add((0, "<separator>"));
        }

        public void Dispose()
        {
        }
    }

    private sealed class FakeNative : TrayIconService.ITrayIconNative
    {
        public bool AddCalled { get; private set; }
        public bool RemoveCalled { get; private set; }

        public FakePopupMenu LastMenu { get; private set; } = new FakePopupMenu();

        public IntPtr CreateMessageWindow(Delegate wndProc)
        {
            return new IntPtr(42);
        }

        public void DestroyMessageWindow(IntPtr hwnd)
        {
        }

        public IntPtr LoadIconFromFile(string path)
        {
            return new IntPtr(77);
        }

        public IntPtr LoadApplicationIcon()
        {
            return new IntPtr(88);
        }

        public void DestroyIcon(IntPtr hIcon)
        {
        }

        public bool AddNotifyIcon(IntPtr hwnd, uint iconId, uint callbackMessage, IntPtr hIcon, string tooltip)
        {
            AddCalled = true;
            return true;
        }

        public bool RemoveNotifyIcon(IntPtr hwnd, uint iconId)
        {
            RemoveCalled = true;
            return true;
        }

        public IntPtr DefWindowProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            return IntPtr.Zero;
        }

        public bool TryGetCursorPos(out TrayIconService.POINT pt)
        {
            pt = new TrayIconService.POINT { X = 1, Y = 2 };
            return true;
        }

        public TrayIconService.ITrayPopupMenu CreatePopupMenu()
        {
            LastMenu = new FakePopupMenu();
            return LastMenu;
        }

        public void TrackPopupMenu(IntPtr hMenu, int x, int y, IntPtr hwnd)
        {
        }

        public void SetForegroundWindow(IntPtr hwnd)
        {
        }
    }

    [TestMethod]
    public void Initialize_WhenCalledMultipleTimes_DoesNotThrow()
    {
        var tempFolder = Path.Combine(Path.GetTempPath(), $"TheMadArchivist_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempFolder);

        try
        {
            var native = new FakeNative();
            var service = new TrayIconService(
                getIconFilePath: () => Path.Combine(tempFolder, "missing.ico"),
                native: native);

            service.Initialize();
            service.Initialize();

            Assert.IsTrue(service.IsInitialized);
            Assert.IsTrue(native.AddCalled);

            service.Dispose();
            service.Dispose();

            Assert.IsTrue(native.RemoveCalled);

            var texts = native.LastMenu.Items.Select(i => i.text).ToArray();
            CollectionAssert.Contains(texts, "Open");
            CollectionAssert.Contains(texts, "Exit");

            var openIndex = Array.IndexOf(texts, "Open");
            var exitIndex = Array.IndexOf(texts, "Exit");
            Assert.IsGreaterThanOrEqualTo(0, openIndex);
            Assert.IsGreaterThan(openIndex, exitIndex);
            CollectionAssert.Contains(texts, "<separator>");
            Assert.AreEqual("<separator>", texts[openIndex + 1]);
        }
        finally
        {
            if (Directory.Exists(tempFolder))
            {
                Directory.Delete(tempFolder, recursive: true);
            }
        }
    }
}
