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

        string privateReply = "";

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

        string GetLangString(long chatId, string key, params object[] args)
        {
            return Program.GetLangString(chatId, key, args);
        }

        async Task PrivateReply(long groupId, IReplyMarkup replyMarkup = null, ParseMode _parseMode = ParseMode.Markdown)
        {
            await Program.SendMessage(groupId, privateReply, replyMarkup, _parseMode);
            privateReply += ""; //Reset reply string
        }
    }
}
