﻿using DesktopWindowManager.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Vanara.PInvoke;
using WindowsDesktop;

[assembly: InternalsVisibleTo("AmethystWindowsSystrayTests")]
namespace AmethystWindowsSystray
{
    class DesktopWindowsManager
    {
        public Dictionary<Pair<VirtualDesktop, HMONITOR>, Layout> Layouts;
        public Dictionary<Pair<VirtualDesktop, HMONITOR>, ObservableCollection<DesktopWindow>> Windows;
        public Dictionary<Pair<VirtualDesktop, HMONITOR>, bool> WindowsSubcribed;
        private int padding;

        public int Padding
        {
            get { return padding; }
            set {
                padding = value;
                Draw();
            }
        }

        public DesktopWindowsManager()
        {
            this.padding = Properties.Settings.Default.Padding;
            this.Layouts = new Dictionary<Pair<VirtualDesktop, HMONITOR>, Layout>();
            this.Windows = new Dictionary<Pair<VirtualDesktop, HMONITOR>, ObservableCollection<DesktopWindow>>();
            this.WindowsSubcribed = new Dictionary<Pair<VirtualDesktop, HMONITOR>, bool>();
        }

        public void AddWindow(DesktopWindow desktopWindow)
        {
            Windows[new Pair<VirtualDesktop, HMONITOR>(desktopWindow.VirtualDesktop, desktopWindow.MonitorHandle)].Add(desktopWindow);
        }

        public void RemoveWindow(DesktopWindow desktopWindow)
        {
            Windows[new Pair<VirtualDesktop, HMONITOR>(desktopWindow.VirtualDesktop, desktopWindow.MonitorHandle)].Remove(desktopWindow);
        }

        public void RepositionWindow(DesktopWindow oldDesktopWindow, DesktopWindow newDesktopWindow)
        {
            RemoveWindow(oldDesktopWindow);
            AddWindow(newDesktopWindow);
        }

        public DesktopWindow FindWindow(HWND hWND)
        {
            List<DesktopWindow> desktopWindows = new List<DesktopWindow>();
            foreach (var desktopMonitor in Windows)
            {
                desktopWindows.AddRange(Windows[new Pair<VirtualDesktop, HMONITOR>(desktopMonitor.Key.Item1, desktopMonitor.Key.Item2)].Where(window => window.Window == hWND));
            }
            return desktopWindows.FirstOrDefault();
        }

        public DesktopWindow GetWindowByHandlers(HWND hWND, HMONITOR hMONITOR, VirtualDesktop desktop)
        {
            return Windows[new Pair<VirtualDesktop, HMONITOR>(desktop, hMONITOR)].FirstOrDefault(window => window.Window == hWND);
        }

        public void SaveLayouts()
        {
            Properties.Settings.Default.Layouts = JsonConvert.SerializeObject(Layouts.ToList(), Formatting.Indented, new LayoutsConverter());
            Console.WriteLine(Properties.Settings.Default.Layouts.ToString());
            Properties.Settings.Default.Save();
        }

        public void ReadLayouts()
        {
            Console.WriteLine(Properties.Settings.Default.Layouts.ToString());
            Layouts = JsonConvert.DeserializeObject<List<KeyValuePair<Pair<VirtualDesktop, HMONITOR>, Layout>>>(
                Properties.Settings.Default.Layouts.ToString(), new LayoutsConverter()
                ).ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        public IEnumerable<Tuple<int, int, int, int>> GridGenerator(int mWidth, int mHeight, int windowsCount, Layout layout)
        {
            if (layout == Layout.Horizontal)
            {
                int horizSize = mWidth / windowsCount;
                int j = 0;
                for (int i = 0; i < windowsCount; i++)
                {
                    yield return new Tuple<int, int, int, int>(i * horizSize, j, horizSize, mHeight);
                }
            }
            else if (layout == Layout.Vertical)
            {
                int vertSize = mHeight / windowsCount;
                int j = 0;
                for (int i = 0; i < windowsCount; i++)
                {
                    yield return new Tuple<int, int, int, int>(j, i * vertSize, mWidth, vertSize);
                }
            }
            else if (layout == Layout.HorizGrid)
            {
                int i = 0;
                int j = 0;
                int horizStep = Math.Max((int)Math.Sqrt(windowsCount), 1);
                int vertStep = Math.Max(windowsCount / horizStep, 1);
                int tiles = horizStep * vertStep;
                int horizSize = mWidth / horizStep;
                int vertSize = mHeight / vertStep;
                bool isFirstLine = true;

                if (windowsCount != tiles || windowsCount == 3)
                {
                    if (windowsCount == 3)
                    {
                        vertStep--;
                        vertSize = mHeight / vertStep;
                    }

                    while (windowsCount > 0)
                    {
                        yield return new Tuple<int, int, int, int>(i * horizSize, j * vertSize, horizSize, vertSize);
                        i++;
                        if (i >= horizStep)
                        {
                            i = 0;
                            j++;
                        }
                        if (j == vertStep - 1 && isFirstLine)
                        {
                            horizStep++;
                            horizSize = mWidth / horizStep;
                            isFirstLine = false;
                        }
                        windowsCount--;
                    }
                }
                else
                {
                    while (windowsCount > 0)
                    {
                        yield return new Tuple<int, int, int, int>(i * horizSize, j * vertSize, horizSize, vertSize);
                        i++;
                        if (i >= horizStep)
                        {
                            i = 0;
                            j++;
                        }
                        windowsCount--;
                    }
                }
            }
            else if (layout == Layout.VertGrid)
            {
                int i = 0;
                int j = 0;
                int vertStep = Math.Max((int)Math.Sqrt(windowsCount), 1);
                int horizStep = Math.Max(windowsCount / vertStep, 1);
                int tiles = horizStep * vertStep;
                int vertSize = mHeight / vertStep;
                int horizSize = mWidth / horizStep;
                bool isFirstLine = true;

                if (windowsCount != tiles || windowsCount == 3)
                {
                    if (windowsCount == 3)
                    {
                        horizStep--;
                        horizSize = mWidth / horizStep;
                    }

                    while (windowsCount > 0)
                    {
                        yield return new Tuple<int, int, int, int>(i * horizSize, j * vertSize, horizSize, vertSize);
                        j++;
                        if (j >= vertStep)
                        {
                            j = 0;
                            i++;
                        }
                        if (i == horizStep - 1 && isFirstLine)
                        {
                            vertStep++;
                            vertSize = mHeight / vertStep;
                            isFirstLine = false;
                        }
                        windowsCount--;
                    }
                }
                else
                {
                    while (windowsCount > 0)
                    {
                        yield return new Tuple<int, int, int, int>(i * horizSize, j * vertSize, horizSize, vertSize);
                        j++;
                        if (j >= vertStep)
                        {
                            j = 0;
                            i++;
                        }
                        windowsCount--;
                    }
                }
            }
            else if (layout == Layout.Monocle)
            {
                for (int i = 0; i < windowsCount; i++)
                {
                    yield return new Tuple<int, int, int, int>(0, 0, mWidth, mHeight);
                }
            }
            else if (layout == Layout.Wide)
            {
                if (windowsCount == 1) yield return new Tuple<int, int, int, int>(0, 0, mWidth, mHeight);
                else
                {
                    int size = mWidth / (windowsCount - 1);
                    for (int i = 0; i < windowsCount - 1; i++)
                    {
                        if (i == 0) yield return new Tuple<int, int, int, int>(0, 0, mWidth, mHeight / 2);
                        yield return new Tuple<int, int, int, int>(i * size, mHeight / 2, size, mHeight / 2);
                    }
                } 
            }
            else if (layout == Layout.Tall)
            {
                if (windowsCount == 1) yield return new Tuple<int, int, int, int>(0, 0, mWidth, mHeight);
                else
                {
                    int size = mHeight / (windowsCount - 1);
                    for (int i = 0; i < windowsCount - 1; i++)
                    {
                        if (i == 0) yield return new Tuple<int, int, int, int>(0, 0, mWidth / 2, mHeight);
                        yield return new Tuple<int, int, int, int>(mWidth / 2, i * size, mWidth / 2, size);
                    }
                }
            }
        }

        public Layout RotateLayouts(Layout currentLayout)
        {
            IEnumerable<Layout> values = Enum.GetValues(typeof(Layout)).Cast<Layout>();
            if (currentLayout == values.Max())
            {
                return Layout.Horizontal;
            }
            else
            {
                return ++currentLayout;
            }
        }

        public void Draw(DesktopWindow dekstopWindow)
        {
            Pair<VirtualDesktop, HMONITOR> key = new Pair<VirtualDesktop, HMONITOR>(dekstopWindow.VirtualDesktop, dekstopWindow.MonitorHandle);
            ObservableCollection<DesktopWindow> windows = Windows[key];
            KeyValuePair<Pair<VirtualDesktop, HMONITOR>, ObservableCollection<DesktopWindow>> desktopMonitor = new KeyValuePair<Pair<VirtualDesktop, HMONITOR>, ObservableCollection<DesktopWindow>>(key, windows);
            float ScreenScalingFactorVert;
            int mX, mY;
            IEnumerable<Tuple<int, int, int, int>> gridGenerator;
            DrawMonitor(desktopMonitor, out ScreenScalingFactorVert, out mX, out mY, out gridGenerator);

            foreach (var w in desktopMonitor.Value.Select((value, i) => new Tuple<int, DesktopWindow>(i, value)))
            {
                DrawWindow(ScreenScalingFactorVert, mX, mY, gridGenerator, w);
            }
        }

        public void Draw(Pair<VirtualDesktop, HMONITOR> key)
        {
            ObservableCollection<DesktopWindow> windows = Windows[key];
            KeyValuePair<Pair<VirtualDesktop, HMONITOR>, ObservableCollection<DesktopWindow>> desktopMonitor = new KeyValuePair<Pair<VirtualDesktop, HMONITOR>, ObservableCollection<DesktopWindow>>(key, windows);
            float ScreenScalingFactorVert;
            int mX, mY;
            IEnumerable<Tuple<int, int, int, int>> gridGenerator;
            DrawMonitor(desktopMonitor, out ScreenScalingFactorVert, out mX, out mY, out gridGenerator);

            foreach (var w in desktopMonitor.Value.Select((value, i) => new Tuple<int, DesktopWindow>(i, value)))
            {
                DrawWindow(ScreenScalingFactorVert, mX, mY, gridGenerator, w);
            }
        }

        public void Draw()
        {
            foreach (var desktopMonitor in Windows)
            {
                float ScreenScalingFactorVert;
                int mX, mY;
                IEnumerable<Tuple<int, int, int, int>> gridGenerator;
                DrawMonitor(desktopMonitor, out ScreenScalingFactorVert, out mX, out mY, out gridGenerator);

                foreach (var w in desktopMonitor.Value.Select((value, i) => new Tuple<int, DesktopWindow>(i, value)))
                {
                    DrawWindow(ScreenScalingFactorVert, mX, mY, gridGenerator, w);
                }
            }
        }

        private void DrawMonitor(KeyValuePair<Pair<VirtualDesktop, HMONITOR>, ObservableCollection<DesktopWindow>> desktopMonitor, out float ScreenScalingFactorVert, out int mX, out int mY, out IEnumerable<Tuple<int, int, int, int>> gridGenerator)
        {
            HMONITOR m = desktopMonitor.Key.Item2;
            int windowsCount = desktopMonitor.Value.Count();

            User32.MONITORINFOEX info = new User32.MONITORINFOEX();
            info.cbSize = (uint)Marshal.SizeOf(info);
            User32.GetMonitorInfo(m, ref info);

            Gdi32.SafeHDC hdc = Gdi32.CreateDC(info.szDevice);
            int LogicalScreenHeight = Gdi32.GetDeviceCaps(hdc, Gdi32.DeviceCap.VERTRES);
            int PhysicalScreenHeight = Gdi32.GetDeviceCaps(hdc, Gdi32.DeviceCap.DESKTOPVERTRES);
            int LogicalScreenWidth = Gdi32.GetDeviceCaps(hdc, Gdi32.DeviceCap.HORZRES);
            int PhysicalScreenWidth = Gdi32.GetDeviceCaps(hdc, Gdi32.DeviceCap.DESKTOPHORZRES);
            hdc.Close();

            float ScreenScalingFactorHoriz = (float)PhysicalScreenWidth / (float)LogicalScreenWidth;
            ScreenScalingFactorVert = (float)PhysicalScreenHeight / (float)LogicalScreenHeight;
            mX = info.rcMonitor.X;
            mY = info.rcMonitor.Y;
            int mWidth = info.rcWork.Width;
            int mHeight = info.rcWork.Height;

            Layout mCurrentLayout;
            try
            {
                mCurrentLayout = Layouts[desktopMonitor.Key];
            }
            catch
            {
                Layouts.Add(desktopMonitor.Key, Layout.Tall);
                mCurrentLayout = Layouts[desktopMonitor.Key];
            }

            gridGenerator = GridGenerator(mWidth, mHeight, windowsCount, mCurrentLayout);
        }

        private void DrawWindow(float ScreenScalingFactorVert, int mX, int mY, IEnumerable<Tuple<int, int, int, int>> gridGenerator, Tuple<int, DesktopWindow> w)
        {
            RECT adjustedSize = new RECT(new Rectangle(
                gridGenerator.ToArray()[w.Item1].Item1,
                gridGenerator.ToArray()[w.Item1].Item2,
                gridGenerator.ToArray()[w.Item1].Item3,
                gridGenerator.ToArray()[w.Item1].Item4
                ));

            User32.AdjustWindowRectExForDpi(
                ref adjustedSize,
                w.Item2.Info.dwStyle,
                false, w.Item2.Info.dwExStyle,
                (uint)(ScreenScalingFactorVert / 96)
                );

            //Prepare the WINDOWPLACEMENT structure.
            User32.WINDOWPLACEMENT placement = new User32.WINDOWPLACEMENT();
            placement.length = (uint)Marshal.SizeOf(placement);

            //Get the window's current placement.
            User32.GetWindowPlacement(w.Item2.Window, ref placement);
            placement.showCmd = ShowWindowCommand.SW_RESTORE;

            //Perform the action.
            User32.SetWindowPlacement(w.Item2.Window, ref placement);

            User32.SetWindowPos(
                w.Item2.Window,
                HWND.HWND_NOTOPMOST,
                adjustedSize.X + mX - w.Item2.Borders.left + Padding,
                adjustedSize.Y + mY - w.Item2.Borders.top + Padding,
                0,
                0,
                User32.SetWindowPosFlags.SWP_NOACTIVATE
                );

            Console.WriteLine(adjustedSize.X + mX - w.Item2.Borders.left + Padding);

            User32.SetWindowPos(
                w.Item2.Window,
                HWND.HWND_NOTOPMOST,
                0,
                0,
                adjustedSize.Width + w.Item2.Borders.left + w.Item2.Borders.right - 2 * Padding,
                adjustedSize.Height + w.Item2.Borders.top + w.Item2.Borders.bottom - 2 * Padding,
                User32.SetWindowPosFlags.SWP_NOMOVE
                );

            w.Item2.GetInfo();
        }
    }
}
