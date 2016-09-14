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
        public Resources cityResources;
        public Resources resourceRegen;
        public Dictionary<ResourceType, byte> lvlResourceRegen = new Dictionary<ResourceType, byte>();

        RefResources refResources = new RefResources();
        public PlayerDetails playerDetails = new PlayerDetails();

        public int msgId = 0;
        public Stack<InlineKeyboardMarkup> menuHistory = new Stack<InlineKeyboardMarkup>(10);
        public Stack<string> replyHistory = new Stack<string>(10);
        
        /* Unimplemented yet
         * public Army cityArmy;
         * public Upgrades cityUpgrades;
         */

        public City(long telegramId, string firstName, string lastName, string cityName, long groupId)
        {
            playerDetails = new PlayerDetails(telegramId, firstName, lastName, cityName, groupId);

            InitCity();
        }

        /// <summary>
        /// City initializer
        /// </summary>
        void InitCity()
        {
            cityResources = refResources.StartingResources;
            lvlResourceRegen = refResources.DefaultLvlResourceRegen;

            //init resourceRegen
            resourceRegen.Gold = refResources.GoldRegenDefault;
            resourceRegen.Wood = refResources.ResourceRegen[ResourceType.Wood][lvlResourceRegen[ResourceType.Wood]];
            resourceRegen.Stone = refResources.ResourceRegen[ResourceType.Stone][lvlResourceRegen[ResourceType.Stone]];
            resourceRegen.Mithril = refResources.ResourceRegen[ResourceType.Mithril][lvlResourceRegen[ResourceType.Mithril]];
        }

		public void UpdateRegen()
		{
			resourceRegen.Gold = refResources.GoldRegenDefault;
			resourceRegen.Wood = refResources.ResourceRegen[ResourceType.Wood][lvlResourceRegen[ResourceType.Wood]];
			resourceRegen.Stone = refResources.ResourceRegen[ResourceType.Stone][lvlResourceRegen[ResourceType.Stone]];
			resourceRegen.Mithril = refResources.ResourceRegen[ResourceType.Mithril][lvlResourceRegen[ResourceType.Mithril]];
		}

        /// <summary>
        /// Saves reply history for 'Back' button
        /// </summary>
        /// <param name="menu">InlineKeyboardMarkup to save</param>
        /// <param name="reply">Reply string to save</param>
        public void AddReplyHistory(InlineKeyboardMarkup menu, string reply)
        {
            menuHistory.Push(menu);
            replyHistory.Push(reply);
        }

        string GetLangString(long chatId, string key, params object[] args)
        {
            return Program.GetLangString(chatId, key, args);
        }
    }
}
