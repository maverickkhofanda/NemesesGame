using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using ConsoleApplication2;

namespace ConsoleApplication2
{
    public class Game
    {
        Dictionary<long, string> playersDict = new Dictionary<long, string>();
        
        public Dictionary<long, string> PlayersDict { get; }
        public Game(long telegramId, string firstName)
        {
            AddPlayer(telegramId, firstName);
        }

        public void AddPlayer(long telegramId, string firstName)
        {
            if (playersDict.ContainsKey(telegramId))
            {
                Console.WriteLine("Player joins again error: unimplemented yet!");
            } else {
                playersDict.Add(telegramId, firstName);                
            }

            Console.WriteLine("{0} ({1}) has joined the game!", firstName, telegramId);
            
        }

        public string IsItRunning()
        {
            string s = "New game is made in this lobby!";
            return s;
        }
    }
}
