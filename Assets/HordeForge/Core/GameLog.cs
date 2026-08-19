using System;
using System.Collections.Generic;

namespace HordeForge.Core
{
    public enum LogSeverity
    {
        Info = 0,
        Warning = 1,
        Alert = 2
    }

    public struct LogEntry
    {
        public string Message;
        public LogSeverity Severity;

        public LogEntry(string message, LogSeverity severity)
        {
            Message = message;
            Severity = severity;
        }
    }

    /// <summary>
    /// Kurzes Ereignisprotokoll fuer das HUD – "Welle 4 gestartet", "Inventar voll",
    /// "Nahrung wird knapp". Bewusst nur ein Ringpuffer, kein Nachrichtensystem.
    /// </summary>
    public sealed class GameLog
    {
        private readonly List<LogEntry> _entries = new List<LogEntry>();

        public int Capacity = 40;

        public event Action<LogEntry> Added;

        public IReadOnlyList<LogEntry> Entries
        {
            get { return _entries; }
        }

        public void Info(string message)
        {
            Write(message, LogSeverity.Info);
        }

        public void Warn(string message)
        {
            Write(message, LogSeverity.Warning);
        }

        public void Alert(string message)
        {
            Write(message, LogSeverity.Alert);
        }

        private void Write(string message, LogSeverity severity)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            LogEntry entry = new LogEntry(message, severity);
            _entries.Add(entry);

            while (_entries.Count > Capacity)
            {
                _entries.RemoveAt(0);
            }

            Action<LogEntry> handler = Added;
            if (handler != null)
            {
                handler(entry);
            }
        }

        public void Clear()
        {
            _entries.Clear();
        }
    }
}
