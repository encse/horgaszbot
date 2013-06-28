using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace horgaszbot
{
    class WowLocator
    {
        
        static IEnumerable<IntPtr> EnumerateProcessWindowHandles(int processId)
        {
            var handles = new List<IntPtr>();

            foreach (ProcessThread thread in Process.GetProcessById(processId).Threads)
                User32.EnumThreadWindows(thread.Id,
                                         (hWnd, lParam) => { handles.Add(hWnd); return true; }, IntPtr.Zero);

            return handles;
        }



        public static IntPtr HwndFind()
        {
            var process = Process.GetProcesses().First(x => x.ProcessName.StartsWith("Wow-64"));
            return EnumerateProcessWindowHandles(process.Id).Where(hwnd =>
                                                                       {
                                                                           var stringBuilder = new StringBuilder(256);
                                                                           User32.GetWindowText(hwnd, stringBuilder, stringBuilder.Capacity);
                                                                           return stringBuilder.ToString() == "World of Warcraft";
                                                                       }).First();
        }
    }
}