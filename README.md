# TwitchTTS
 A simple TTS Twitch bot that lets viewers control their voices

## Usage
When the program starts, a terminal will open where the user can enter commands for testing voices, controlling which voices are enable, and controlling which viewers' messages are read.
You may type `help` to get a list of commands or `help` followed by a specific command to get more details for it.
Typing `exit` will close the program.

Type `join` to enable monitoring chat for a specific stream. Example: `join nintendo`. To leave a channel, type `leave`.

When viewers send messages to channels that are being monitored, the messsages enter a queue and will be read out loud in the order that they were recieved.

If the queue grows too large, you may reset the queue by typing `clear`.

To test speaking messages, type `say` followed by your message.

To change the voice used, include `!voice#` in your message prior to the text to be read in that voice, replacing the pound symbol with a number. Viewers may also use this command to change their voices.
Example: `say !voice2 This is voice 2`
The available voices are based on the languages packs installed on your Windows machine.

When a user's message is first recieved, that user will be assigned a voice that will be used for all of their messages.

Voices may be disabled or re-enabled from being assigned by using the `disable` or `enable` commands. Example: `disable 3` will disable the third available voice.

Users may be muted to prevent their messages from being read outloud with the `silence` command. Example: `silence twitchfan42`. Users can be unsilenced with the `unsilence` command.

To check what voice a user has been assigned, use the `check` command. Example: `check twitchfan42`

To reassign a voice to a user, use the `assign` command. Example: `assign twitchfan42 3`

## Style modifiers
When a message is read, certain keywords may be used in the message to alter how it's spoken. These keywords begin with an exclamation point followed by the keyword without any spaces between them.

- `!fast` makes the message faster than normal.
- `!xfast` makes the message the fastest.
- `!slow` makes the message slower than normal.
- `!xslow` makes the message the slowest.
- `!strong` makes the message with a strong emphasis.
- `!moderate` makes the message with a moderate emphasis.
- `!reduced` makes the message with a reduced emphasis.
- `!soft` makes the message with at a softer volume.
- `!loud` makes the message with at a louder volume.

## Sounds
The program will play certain sounds to help signify where a user's message starts and ends. This is to mitigate users attempting to impersonating others.

The following .wav files will be used if they exist next to the program executable file.

- `start.wav` - Plays when the program finishes connecting to Twitch and is ready to receive commands. It also plays after the message queue is cleared when the `clear` command is used.
- `messagebegin.wav` - Plays before a message is read after announcing the username of the viewer.
- `messageend.wav` - Plays when a viewers message concludes.
