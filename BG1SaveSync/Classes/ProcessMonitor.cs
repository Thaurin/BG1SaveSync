using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BG1SaveSync.Classes
{
    public static class ProcessMonitor
    {
        public static event EventHandler ProcessClosed;

        public static void MonitorForStart(string processName)
        {
            Thread thread = new Thread(() =>
            {
                bool started = false;
                do
                {
                    Process[] processes = Process.GetProcessesByName(processName);
                    if (processes.Length > 0)
                    {
                        MonitorForExit(processes[0]);
                        started = true;
                    }
                    else
                    {
                        Thread.Sleep(1000);
                    }
                } while (!started);
            });

            thread.Start();
        }

        public static void MonitorForExit(Process process)
        {
            Thread thread = new Thread(() =>
            {
                process.WaitForExit();
                OnProcessClosed(EventArgs.Empty);
            });
            thread.Start();
        }

        private static void OnProcessClosed(EventArgs e)
        {
            ProcessClosed?.Invoke(null, e);
        }
    }
}
