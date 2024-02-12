using System;
using System.Diagnostics;

namespace StarTrad.Tool
{
	public static class Logger
	{
		private static TextWriterTraceListener fileListener = new TextWriterTraceListener(App.workingDirectoryPath + "logfile.txt");
		public static void Setup()
		{
			Trace.Listeners.Add(fileListener);
			Trace.WriteLine("\n");
		}

		public static void LogInformation(string message)
		{
			Trace.TraceInformation($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [INFO] {message}");
			Trace.Flush();
		}

		public static void LogWarning(string message)
		{
			Trace.TraceWarning($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [WARN] {message}");
			Trace.Flush();
		}

		public static void LogError(string? message)
		{
			if (message == null)
			{
				return;
			}

			Trace.TraceWarning($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [ERROR] {message}");
			Trace.Flush();
		}

		public static void LogError(Exception exception)
		{
			Trace.TraceError($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [ERROR] {exception.Message}\n{exception}");
			Trace.Flush();
		}
	}
}
