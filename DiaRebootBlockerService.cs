using System;
using System.Diagnostics;
using System.ServiceProcess;
using System.Timers;
using Microsoft.Win32;
using Microsoft.VisualBasic.Devices;

namespace DiaRebootBlocker
{
    public partial class DiaRebootBlockerService : ServiceBase
    {
        private readonly EventLog eventLog = new EventLog();
        private readonly Timer checkTimer = new Timer();

        private const string eventSourceName = "DiaRebootBlockerService";

        private const int hourLimitHome = 12;
        private const int hourLimitPro = 18;

        public DiaRebootBlockerService()
        {
            InitializeComponent();

            if (!EventLog.SourceExists(eventSourceName))
            {
                EventLog.CreateEventSource(eventSourceName, "Application");
            }
            eventLog.Source = eventSourceName;
            eventLog.Log = "Application";

            checkTimer.Interval = 60 * 1000;
            checkTimer.Elapsed += OnCheckTimer;

        }

        private bool UpdateHours(bool forceLogs = false)
        {
            const string settingsPath = "SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings";

            var key = Registry.LocalMachine.CreateSubKey(settingsPath);
            if (key == null)
            {
                eventLog.WriteEntry("Failed to open the registry key:\n\n" + settingsPath, 
                    EventLogEntryType.Error);
                return false;
            }

            var start = -1;
            var end = -1;

            if (key.GetValueKind("ActiveHoursStart") == RegistryValueKind.DWord)
            {
                start = (int)key.GetValue("ActiveHoursStart");
            }
            if (key.GetValueKind("ActiveHoursEnd") == RegistryValueKind.DWord)
            {
                end = (int)key.GetValue("ActiveHoursEnd");
            }

            var newStart = (DateTime.Now.Hour - 2 + 24) % 24;
            var newEnd = (newStart + hourLimitHome) % 24;

            string msg;
            if (start == -1 || end == -1)
            {
                msg = "Current settings invalid";
            } 
            else
            {
                msg = $"Current active hours range: {start}:00 to {end}:00";
            }

            if (start != newStart || end != newEnd)
            {
                msg += "\n";
                msg += $"New active hours range: {newStart}:00 to {newEnd}:00";
                forceLogs = true;

                key.SetValue("ActiveHoursStart", newStart, RegistryValueKind.DWord);
                key.SetValue("ActiveHoursEnd", newEnd, RegistryValueKind.DWord);
            }

            if (forceLogs || ((newStart - start + 24) % 24 != 1))
            {
                eventLog.WriteEntry(msg);
            }

            return true;
        }

        private void MildPanic()
        {
            eventLog.WriteEntry("Failed to update active hours!", EventLogEntryType.Warning);
        }

        private void Panic()
        {
            eventLog.WriteEntry("Failed to update active hours! Something is wrong with the service.", EventLogEntryType.Error);
            Stop();
        }

        private void OnCheckTimer(object sender, ElapsedEventArgs e)
        {
            if (!UpdateHours())
            {
                MildPanic();
            }
        }

        protected override void OnStart(string[] args)
        {
            checkTimer.Start();

            var computerInfo = new ComputerInfo();
            eventLog.WriteEntry("Starting on " + computerInfo.OSFullName);

            if (!UpdateHours(true))
            {
                Panic();
            }
        }

        protected override void OnStop()
        {
            eventLog.WriteEntry("Stopping");
            checkTimer.Stop();
        }
    }
}
