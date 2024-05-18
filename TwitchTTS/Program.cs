using System.Speech.Synthesis;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;

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

	class Bot
	{
		TwitchClient client;
		SpeechSynthesizer speech;
		readonly List<VoiceInfo> voices = new();
		HashSet<VoiceInfo> defaultVoices = new();
		VoiceInfo narrator;
		Dictionary<string, VoiceInfo> userVoices = new();
		HashSet<string> silencedUsers = new();

		Dictionary<string, string> replacementPhrases = new();

		Queue<ChatMessage> messageQueue = new();

		StreamWriter logFile;

		public Bot(string? channel = null)
		{
			logFile = File.AppendText("log.txt");
			Log("TwitchTTS starting.");

			speech = new();
			speech.InjectOneCoreVoices();
			speech.SetOutputToDefaultAudioDevice();
			speech.SpeakCompleted += Speech_OnSpeakCompleted;
			voices = speech.GetInstalledVoices().Select(voice => voice.VoiceInfo).ToList();
			defaultVoices = new HashSet<VoiceInfo>(voices);
			narrator = voices.FirstOrDefault();

			Console.WriteLine(voices.Count + " voices loaded");

			ConnectionCredentials credentials = new ConnectionCredentials("justinfan" + new Random().Next(1000, 79999), "");
			client = new TwitchClient();
			client.Initialize(credentials, channel);

			client.OnLog += Client_OnLog;
			client.OnJoinedChannel += Client_OnJoinedChannel;
			client.OnLeftChannel += Client_OnLeaveChannel;
			client.OnMessageReceived += Client_OnMessageReceived;
			client.OnConnected += Client_OnConnected;

			client.Connect();
		}

		~Bot()
		{
			Log("TwitchTTS closing.");
			logFile.Close();
		}

		private void Log(string message)
		{
			logFile.WriteLine($"{DateTime.Now}: {message}");
			logFile.Flush();
		}

		private void Client_OnLog(object sender, OnLogArgs e)
		{
			// Log($"{e.BotUsername} - {e.Data}");
			// Console.WriteLine($"{e.DateTime}: {e.BotUsername} - {e.Data}");
		}

		private void Client_OnConnected(object sender, OnConnectedArgs e)
		{
			PlaySound("./start.wav");
			Log($"Connected to {e.AutoJoinChannel}");
		}

		private void Client_OnJoinedChannel(object sender, OnJoinedChannelArgs e)
		{
			PlaySound("./start.wav");
			Log("Joined channel " + e.Channel);
		}
		private void Client_OnLeaveChannel(object sender, OnLeftChannelArgs e)
		{
			PlaySound("./start.wav");
			Log("Left channel " + e.Channel);
		}

		private void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
		{
			GetUserVoice(e.ChatMessage.Username);
			Log(e.ChatMessage.Username + ": " + e.ChatMessage.Message);

			messageQueue.Enqueue(e.ChatMessage);
			SpeakNextMessage();
		}

		private void Speech_OnSpeakCompleted(object sender, SpeakCompletedEventArgs e)
		{
			SpeakNextMessage();
		}

		private void SpeakNextMessage()
		{
			if (speech.State == SynthesizerState.Ready && messageQueue.TryDequeue(out ChatMessage chatMessage))
			{
				// If the chat message is from a silenced user or a channel that was left, get the next message.
				while (chatMessage != null && (silencedUsers.Contains(chatMessage.Username) || !client.JoinedChannels.Any(c => c.Channel == chatMessage.Channel)))
					messageQueue.TryDequeue(out chatMessage);

				if (chatMessage != null)
				{
					PromptBuilder prompt = new PromptBuilder();
					Narrate(prompt, chatMessage.Username);
					prompt.AppendAudio("./messagebegin.wav");
					string message = replacementPhrases.Aggregate(chatMessage.Message, (message, pair) => message.Replace(pair.Key, pair.Value));
					if (Speak(prompt, message, chatMessage.Username))
					{
						prompt.AppendAudio("./messageend.wav");
						speech.SpeakAsync(prompt);
					}
				}
			}
		}

		private VoiceInfo? GetUserVoice(string username)
		{
			VoiceInfo? voice = null;
			if (username != null && !userVoices.TryGetValue(username, out voice))
			{
				var validVoices = voices.Intersect(defaultVoices).ToList();
				int index = new Random().Next(validVoices.Count);
				voice = validVoices[index];
				userVoices.Add(username, voice);
				Log("Assigning voice " + voices.IndexOf(voice) + " to " + username);
			}
			return voice;
		}

		public void Join(string channel)
		{
			client.JoinChannel(channel);
		}

		public void Leave(string channel)
		{
			client.LeaveChannel(channel);
		}

		public void Reconnect()
		{
			PlaySound("./start.wav");
			client.Reconnect();
		}

		public void Narrate(PromptBuilder prompt, string message)
		{
			prompt.StartVoice(narrator);
			prompt.StartStyle(new PromptStyle(PromptRate.Fast));
			prompt.AppendText(message);
			prompt.EndStyle();
			prompt.EndVoice();
		}

		public bool Speak(PromptBuilder prompt, string message, string? username = null)
		{
			VoiceInfo? voice = null;
			IEnumerable<string> tokens = message.Split(' ');
			if (tokens.FirstOrDefault("").StartsWith("!voice")
				&& int.TryParse(tokens.First().Substring(6), out int index))
			{
				tokens = tokens.Skip(1);
				if (username != null)
					voice = userVoices[username] = voices[index % voices.Count];
				else
					voice = narrator = voices[index % voices.Count];
			}
			else
				voice = GetUserVoice(username) ?? narrator;

			prompt.StartVoice(voice ?? narrator);
			int pushedStyles = 0;
			foreach (string token in tokens)
			{
				switch (token)
				{
					case "!fast": prompt.StartStyle(new PromptStyle(PromptRate.Fast)); pushedStyles++; break;
					case "!xfast": prompt.StartStyle(new PromptStyle(PromptRate.ExtraFast)); pushedStyles++; break;
					case "!slow": prompt.StartStyle(new PromptStyle(PromptRate.Slow)); pushedStyles++; break;
					case "!xslow": prompt.StartStyle(new PromptStyle(PromptRate.ExtraSlow)); pushedStyles++; break;
					case "!strong": prompt.StartStyle(new PromptStyle(PromptEmphasis.Strong)); pushedStyles++; break;
					case "!moderate": prompt.StartStyle(new PromptStyle(PromptEmphasis.Moderate)); pushedStyles++; break;
					case "!reduced": prompt.StartStyle(new PromptStyle(PromptEmphasis.Reduced)); pushedStyles++; break;
					case "!soft": prompt.StartStyle(new PromptStyle(PromptVolume.Soft)); pushedStyles++; break;
					case "!loud": prompt.StartStyle(new PromptStyle(PromptVolume.Loud)); pushedStyles++; break;
					case "!s": if (pushedStyles > 0) { prompt.EndStyle(); pushedStyles--; } break;
					default: prompt.AppendText(token + " "); break;
				}
			}

			while (pushedStyles-- > 0)
				prompt.EndStyle();

			prompt.EndVoice();

			return tokens.Any();
		}

		public void Speak(string message)
		{
			PromptBuilder prompt = new PromptBuilder();
			Speak(prompt, message);
			speech.Speak(prompt);
		}

		public void DisableVoice(int index)
		{
			if (index >= 0 && index < voices.Count && defaultVoices.Count > 1)
			{
				PlaySound("./start.wav");
				Log("Disabling voice " + index);
				defaultVoices.Remove(voices[index]);
				foreach (var user in userVoices.Where(p => !defaultVoices.Contains(p.Value)).ToList())
					userVoices.Remove(user.Key);
			}
		}

		public void EnableVoice(int index)
		{
			if (index >= 0 && index < voices.Count)
			{
				PlaySound("./start.wav");
				defaultVoices.Add(voices[index]);
				Log("Enabling voice " + index);
			}
		}

		public void SilenceUser(string username)
		{
			PlaySound("./start.wav");
			silencedUsers.Add(username);
		}

		public void UnsilenceUser(string username)
		{
			PlaySound("./start.wav");
			silencedUsers.Remove(username);
		}

		public bool GetVoiceIndex(string username, out int index)
		{
			if (userVoices.TryGetValue(username, out VoiceInfo voice))
			{
				index = voices.IndexOf(voice);
				return true;
			}
			else
			{
				index = -1;
				return false;
			}
		}

		public void AssignVoice(string username, int index)
		{
			userVoices[username] = voices[index % voices.Count];
			Log("Assigning voice " + index % voices.Count + " to " + username);
		}

		public void ClearMessageQueue()
		{
			PlaySound("./start.wav");
			messageQueue.Clear();
		}

		public void PlaySound(string file)
		{
			PromptBuilder prompt = new();
			prompt.AppendAudio(file);
			speech.SpeakAsync(prompt);
		}
	}
}
