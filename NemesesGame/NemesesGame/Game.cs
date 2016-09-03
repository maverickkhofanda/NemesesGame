using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NemesesGame;

namespace NemesesGame
{
    public class Game
    {
        private long chatId;

		private int playerCount = 0;

		public Dictionary<long, City> players = new Dictionary<long, City>();

        public Game()
        {

        }

        public string GameHosted()  
        {
            return "New game is made in this lobby!";
        }

		public void PlayerJoin(long telegramId, string firstName, string lastName)
		{
			players.Add(telegramId, new City(telegramId, firstName, lastName));
			playerCount++;
		}

		public void PlayerLeave(long telegramId)
		{
			players.Remove(telegramId);
			playerCount--;
		}

		public int PlayerCount { get { return playerCount; } }
    }
}
