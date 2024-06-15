namespace TwitchTTS
{
	class Program
	{
		static void Main(string[] args)
		{
			Bot bot = new(args.Length > 0 ? args[0] : null);

			Console.WriteLine("Enter a command or type exit to end");
			string command = Console.ReadLine()?.Trim().ToLower();
			while (command != "exit")
			{
				if (command.StartsWith("silence "))
					bot.SilenceUser(command.Substring(8));
				else if (command.StartsWith("unsilence "))
					bot.UnsilenceUser(command.Substring(10));
				else if (command.StartsWith("disable ") && int.TryParse(command.Substring(8), out int index))
					bot.DisableVoice(index);
				else if (command.StartsWith("enable ") && int.TryParse(command.Substring(7), out index))
					bot.EnableVoice(index);
				else if (command.StartsWith("say "))
					bot.Speak(command.Substring(4));
				else if (command.StartsWith("join "))
					bot.Join(command.Substring(5).Trim());
				else if (command.StartsWith("leave "))
					bot.Leave(command.Substring(6).Trim());
				else if (command == "reconnect")
					bot.Reconnect();
				else if (command == "clear")
					bot.ClearMessageQueue();
				else if (command.StartsWith("check "))
				{
					if (bot.GetVoiceIndex(command.Substring(9), out index))
						bot.Speak("Voice " + index);
					else
						bot.Speak("Unassigned");
				}
				else if (command.StartsWith("assign "))
				{
					string[] commands = command.Split(' ');
					if (commands.Length > 2 && int.TryParse(commands[2], out index))
						bot.AssignVoice(commands[1], index);
				}
				else if (command.StartsWith("help"))
				{
					switch (command.Substring(4).Trim().Split(' ')[0])
					{
						case "silence":
							bot.Speak("!fast silence username - Prevents a user's messages from being spoken."); break;
						case "unsilence":
							bot.Speak("!fast unsilence username - Permits a user's messages to be spoken."); break;
						case "disable":
							bot.Speak("!fast disable voice number - Disables a voice from being assigned to users."); break;
						case "enable":
							bot.Speak("!fast enable voice number - Enables a voice to be assigned to users."); break;
						case "say":
							bot.Speak("!fast say message - Speaks out a message as the narrator."); break;
						case "join":
							bot.Speak("!fast join channel - Joins a channel."); break;
						case "leave":
							bot.Speak("!fast leave channel - Leaves a channel."); break;
						case "reconnect":
							bot.Speak("!fast reconnect - Restarts the bot's twitch connection."); break;
						case "clear":
							bot.Speak("!fast clear - Clears the speech message queue."); break;
						case "exit":
							bot.Speak("!fast exit - Quits the program."); break;
						case "check":
							bot.Speak("!fast check username - Identifies the voice assigned to a user."); break;
						case "assign":
							bot.Speak("!fast assign username voice number - Assigns a voice to a specific user."); break;
						default:
							bot.Speak("!fast Commands. join, leave, reconnect, silence, unsilence, disable, enable, exit, clear, check, assign, say. Type any of these commands after help for details.");
							break;
					}
				}
				else
					bot.Speak("!fast Invalid command. Type exit to quit or help for a list of commands");

				command = Console.ReadLine()?.Trim();
			}

			bot.PlaySound("./end.wav");
		}
	}
}
