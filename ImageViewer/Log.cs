using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace ImageViewerDemo
{
    public static class Log
    {
#if DEBUG
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern void OutputDebugString(string message);
#endif

        private static StringBuilder sb = new StringBuilder(2048);

        private static ConsoleColor DefaultBackgroundColor = Console.BackgroundColor;
        private static ConsoleColor DefaultForegroundColor = Console.ForegroundColor;

        public static string LogFilePath { get; private set; }

        private static StreamWriter file_writer;
        private static bool enable_debug_output;

        private static void FileWrite(string content)
        {
            if (file_writer != null)
            {
                file_writer.Write(content);
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(LogFilePath))
                    File.AppendAllText(LogFilePath, content);
            }
        }

        public static void Term()
        {
            var s = file_writer;
            file_writer = null;
            s.Flush();
            s.Close();
        }

        public static string BuildLogMessage(string message, string type, bool new_line, bool time, string prefix)
        {
            lock (sb)
            {
                sb.Clear();

                sb.AppendFormat("[{0} {1}]", (time ? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") : string.Empty), type);

                if (!string.IsNullOrWhiteSpace(prefix))
                    sb.AppendFormat("{0}", prefix);

                sb.AppendFormat(":{0}", message);

                if (new_line)
                    sb.AppendLine();

                return sb.ToString();
            }
        }

        internal static void Output(string message)
        {
#if DEBUG
            OutputDebugString(message);
#endif
            Console.Write(message);
            FileWrite(message);
        }

        internal static void ColorizeConsoleOutput(string message, ConsoleColor f, ConsoleColor b)
        {
            Console.BackgroundColor = b;
            Console.ForegroundColor = f;

            Output(message);

            Console.ResetColor();
        }

        public static void Info(string message, [CallerMemberName]string prefix = "<Unknown Method>")
        {
            var msg = BuildLogMessage(message, "INFO", true, true, prefix);
            Output(msg);
        }

        public static void Debug(string message, [CallerMemberName]string prefix = "<Unknown Method>")
        {
            if (enable_debug_output)
            {
                var msg = BuildLogMessage(message, "DEBUG", true, true, prefix);
                Output(msg);
            }
        }

        public static void Warn(string message, [CallerMemberName]string prefix = "<Unknown Method>")
        {
            var msg = BuildLogMessage(message, "WARN", true, true, prefix);
            ColorizeConsoleOutput(msg, ConsoleColor.Yellow, DefaultBackgroundColor);
        }

        public static void Error(string message, [CallerMemberName]string prefix = "<Unknown Method>")
        {
            var msg = BuildLogMessage(message, "ERROR", true, true, prefix);
            ColorizeConsoleOutput(msg, ConsoleColor.Red, ConsoleColor.Yellow);
        }

        public static void Error(string message, Exception e, [CallerMemberName]string prefix = "<Unknown Method>")
        {
            message = $"{message} , Exception : {Environment.NewLine} {e.Message} {Environment.NewLine} {e.StackTrace}";
            var msg = BuildLogMessage(message, "ERROR", true, true, prefix);
            ColorizeConsoleOutput(msg, ConsoleColor.Red, ConsoleColor.Yellow);
        }
    }
}
