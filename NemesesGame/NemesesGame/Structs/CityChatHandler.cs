using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace NemesesGame
{
    public enum ReplyType { status, command, general }

    public class CityChatHandler
    {
        public string statusString = "";
        public string cmdString = "";
        public string generalString = "";
		public string messageString = "";
        public List<InlineKeyboardButton> buttons = new List<InlineKeyboardButton>();
        public InlineKeyboardMarkup menu;

        public long playerId;
        public long groupId;
        public int statusMsgId = 0;
        public int cmdMsgId = 0;
		public int messageId = 0;
        
        public Stack<string> statusReplyHistory = new Stack<string>(10);
        public Stack<string> cmdReplyHistory = new Stack<string>(10);
		public Stack<string> messageReplyHistory = new Stack<string>(10);
		public Stack<InlineKeyboardMarkup> menuHistory = new Stack<InlineKeyboardMarkup>(10);
        public int backCount = 0;

        public CityChatHandler(long _playerId, long _groupId)
        {
            playerId = _playerId;
            groupId = _groupId;
        }

        
        public void AddMenuButton(InlineKeyboardButton button)
        {
            buttons.Add(button);
            //Console.WriteLine(button.CallbackData);
        }

        public void SetMenu()
        {
            if (buttons.Count() < 8)
            {
                menu = new InlineKeyboardMarkup(buttons.Select(x => new[] { x }).ToArray());
            }
            else
            {
                SetTwoColMenu();
            }
        }

        public void SetTwoColMenu()
        {
            var twoMenu = new List<InlineKeyboardButton[]>();
            for (var i = 0; i < buttons.Count; i++)
            {
                if (buttons.Count - 1 == i)
                {
                    twoMenu.Add(new[] { buttons[i] });
                }
                else
                    twoMenu.Add(new[] { buttons[i], buttons[i + 1] });
                i++;
            }

            menu = new InlineKeyboardMarkup(twoMenu.ToArray());
        }

        /// <summary>
        /// Saves reply history for 'Back' button
        /// </summary>
        /// <param name="menu">InlineKeyboardMarkup to save</param>
        /// <param name="reply">Reply string to save</param>
        public void AddReplyHistory()
        {
            statusReplyHistory.Push(statusString);
            cmdReplyHistory.Push(cmdString);
			messageReplyHistory.Push(cmdString);
			menuHistory.Push(menu);

            backCount = menuHistory.Count;
        }

        public void ClearReplyHistory()
        {
            statusReplyHistory.Clear();
            cmdReplyHistory.Clear();
			messageReplyHistory.Clear();
            menuHistory.Clear();
        }

        public async Task RemovePreviousChat()
        {
            await Program.EditMessage(playerId, messageId, Program.GetLangString(groupId, "RefreshChat"));
        }
        public void AddReply(ReplyType rType, string replyString)
        {
            if (rType == ReplyType.command)
            {
                cmdString += replyString;
				//Console.WriteLine("Current command string : {0}", cmdString);
            }
            else if (rType == ReplyType.status)
            {
                statusString += replyString;
				//Console.WriteLine("Current status string : {0}", statusString);
			}
            else
            {
                generalString += replyString;
				//Console.WriteLine("Current general string : {0}", generalString);
			}
        }

		public void EditReply(ReplyType rType, string replyString)
		{
			if (rType == ReplyType.command)
			{
				cmdString = replyString;
				//Console.WriteLine("Command edited to : {0}", replyString);
			}
			else if (rType == ReplyType.status)
			{
				statusString = replyString;
				//Console.WriteLine("Status edited to : {0}", replyString);
			}
			else
			{
				generalString = replyString;
				//Console.WriteLine("General edited to : {0}", replyString);
			}
		}

		public void ClearReply(ReplyType rType)
		{
			if (rType == ReplyType.command)
			{
				cmdString = "";
			}
			else if (rType == ReplyType.status)
			{
				statusString = "";
			}
			else
			{
				generalString = "";
			}
		}

        // this better changed to SendMessage
        public async Task SendReply(ParseMode _parseMode = ParseMode.Markdown, bool getNewMsgId = false)
        {
            IReplyMarkup repMarkup = menu;

            if (statusString != "" && cmdString != "")
            {
                // send the reply
                string statusReplyString = statusString;
                string cmdReplyString = cmdString;
                string messageReplyString =
                    statusReplyString +
                    "\r\n--------------\r\n" +
                    cmdReplyString;
                messageString = messageReplyString;

                // clear the strings
                //statusString = "";
                //cmdString = "";

                // try to get the msgId
                if (/*statusMsgId == 0 && cmdMsgId == 0*/ messageId == 0 || getNewMsgId == true)
                {
                    //send the status first, then the command... also get the MsgId here
                    //Message statusMsg =
                    //    await Program.SendMessage(playerId, statusReplyString, _parseMode: _parseMode);
                    //Message cmdMsg =
                    //    await Program.SendMessage(playerId, cmdReplyString, repMarkup, _parseMode);

                    if (getNewMsgId == true)
                    {
                        // deletes previous message
                        await RemovePreviousChat();
                    }
                    Message messageReply =
                        await Program.SendMessage(playerId, messageReplyString, repMarkup, _parseMode);
                    
                    // set the msgId
                    //statusMsgId = statusMsg.MessageId;
                    //cmdMsgId = cmdMsg.MessageId;
                    messageId = messageReply.MessageId;

                    getNewMsgId = false;
                }
                else // we don't need to get the msgId...
                {
                    //await Program.SendMessage(playerId, statusReplyString, _parseMode: _parseMode);
                    //await Program.SendMessage(playerId, cmdReplyString, repMarkup, _parseMode);
                    await Program.SendMessage(playerId, messageReplyString, repMarkup, _parseMode);
                }


                buttons.Clear();
                repMarkup = null;
            }

            if (generalString != "")
            {
                // I think we dont need to get msgId here...
                string replyString = generalString;
                generalString = "";

                await Program.SendMessage(playerId, replyString, _parseMode: _parseMode);
            }
        }

        public async Task EditMessage(IReplyMarkup replyMarkup = null, ParseMode ParseMode = ParseMode.Markdown, bool forceReply = false)
        {
            /*
            if (statusString != "")
            {
                string statusReplyString = statusString;
                statusString = "";

                await Program.EditMessage(playerId, statusMsgId, statusReplyString, _parseMode: ParseMode);
            }

            //if (cmdString != "")
            //{
                string cmdReplyString = cmdString;
                cmdString = "";

                await Program.EditMessage(playerId, cmdMsgId, cmdReplyString, repMarkup: menu, _parseMode: ParseMode);
            //}
			*/

            IReplyMarkup repMarkup = menu;

            if (forceReply == true)
            {
                repMarkup = new ForceReply() { Force = true };
            }

			//Console.WriteLine("EditMessage Called\r\n");
			if (messageString != "")
			{
				string messageReplyString =
					statusString +
					"\r\n-------------------\r\n\r\n" +
					cmdString;

				await Program.EditMessage(playerId, messageId, messageReplyString, repMarkup: repMarkup, _parseMode: ParseMode);
			}

            if (buttons != null)
            {
                buttons.Clear();
                menu = null;
            }
        }
    }
}
