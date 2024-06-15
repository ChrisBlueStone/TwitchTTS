
namespace TwitchTTS
{
	class Logger
	{
		readonly StreamWriter logFile;

		public Logger(string filepath)
		{
			logFile = File.AppendText(filepath);
		}

		~Logger()
		{
			logFile.Close();
		}

		public void Log(string message)
		{
			logFile.WriteLine($"{DateTime.Now}: {message}");
			logFile.Flush();
		}
	}
}
