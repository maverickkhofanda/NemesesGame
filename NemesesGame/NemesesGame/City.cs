using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using NemesesGame;
using Telegram.Bot.Types;

namespace NemesesGame
{
    public class City
    {
        public Resources _resources;
        public Resources resourceRegen;
        public Dictionary<ResourceType, byte> lvlResourceRegen = new Dictionary<ResourceType, byte>();
        public Army _army = new Army();
		public List<long> defeated = new List<long>();

        RefResources refResources = new RefResources();
        public PlayerDetails playerDetails;
        public CityChatHandler chat;
		
        /* Unimplemented yet
         * public Upgrades cityUpgrades;
         */

        public City(long telegramId, string firstName, string lastName, string cityName, long groupId)
        {
            playerDetails = new PlayerDetails(telegramId, firstName, lastName, cityName, groupId);
            chat = new CityChatHandler(playerDetails.telegramId, groupId);

            InitCity();
        }

        /// <summary>
        /// City initializer
        /// </summary>
        void InitCity()
        {
            _resources = refResources.StartingResources;
            lvlResourceRegen = refResources.DefaultLvlResourceRegen;

            //init resourceRegen
            UpdateRegen();
        }

		public void UpdateRegen()
		{
			resourceRegen.Gold = refResources.GoldRegenDefault;
			resourceRegen.Wood = refResources.ResourceRegen[ResourceType.Wood][lvlResourceRegen[ResourceType.Wood]];
			resourceRegen.Stone = refResources.ResourceRegen[ResourceType.Stone][lvlResourceRegen[ResourceType.Stone]];
			resourceRegen.Mithril = refResources.ResourceRegen[ResourceType.Mithril][lvlResourceRegen[ResourceType.Mithril]];
		}
    }
}
