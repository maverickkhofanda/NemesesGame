using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NemesesGame;

namespace NemesesGame
{
    public enum ResourceType { Gold, Wood, Stone, Mithril }

    public class RefResources
    {
        public RefResources()
        {
            InitRefDict();
            StartingResourcesCount();
            //ResourcesRegen0Count();
        }
        Dictionary<ResourceType, byte> defaultLvlResourceRegen = new Dictionary<ResourceType, byte>();
        Dictionary<ResourceType, int[]> resourceRegen = new Dictionary<ResourceType, int[]>();
        Dictionary<ResourceType, Resources[]> upgradeCost = new Dictionary<ResourceType, Resources[]>();
        
        // Not balanced yet!

        // Starting Resources
        int goldStarting = 2000;
        int woodStarting = 250;
        int stoneStarting = 100;
        int mithrilStarting = 50;
        Resources startingResources;

        #region Resource Regen
        int goldRegenDefault = 2000;

        int woodRegen0 = 50;
        int woodRegen1 = 100;
        int woodRegen2 = 300;

        int stoneRegen0 = 20;
        int stoneRegen1 = 60;
        int stoneRegen2 = 120;

        int mithrilRegen0 = 0;
        int mithrilRegen1 = 30;
        int mithrilRegen2 = 100;
        #endregion

        #region ResourceUpgradePrice
        Resources woodUpgrade1 = new Resources(0, 100, 20, 10);
        Resources woodUpgrade2 = new Resources(300, 300, 100, 50);

        Resources stoneUpgrade1 = new Resources(50, 200, 30, 20);
        Resources stoneUpgrade2 = new Resources(500, 300, 100, 100);

        Resources mithrilUpgrade1 = new Resources(250, 100, 30, 20);
        Resources mithrilUpgrade2 = new Resources(1000, 300, 100, 150);
        #endregion

        void InitRefDict ()
        {
            defaultLvlResourceRegen.Add(ResourceType.Wood, 0);
            defaultLvlResourceRegen.Add(ResourceType.Stone, 0);
            defaultLvlResourceRegen.Add(ResourceType.Mithril, 0);

            resourceRegen.Add(ResourceType.Wood, new int[] { woodRegen0, woodRegen1, woodRegen2 });
            resourceRegen.Add(ResourceType.Stone, new int[] { stoneRegen0, stoneRegen1, stoneRegen2 });
            resourceRegen.Add(ResourceType.Mithril, new int[] { mithrilRegen0, mithrilRegen1, mithrilRegen2 });

            upgradeCost.Add(ResourceType.Wood, new Resources[] { new Resources(0, 0, 0, 0), woodUpgrade1, woodUpgrade2 });
            upgradeCost.Add(ResourceType.Stone, new Resources[] { new Resources(0, 0, 0, 0), stoneUpgrade1, stoneUpgrade2 });
            upgradeCost.Add(ResourceType.Mithril, new Resources[] { new Resources(0, 0, 0, 0), mithrilUpgrade1, mithrilUpgrade2 });
        }
        void StartingResourcesCount()
        {
            startingResources.Gold = goldStarting;
            startingResources.Wood = woodStarting;
            startingResources.Stone = stoneStarting;
            startingResources.Mithril = mithrilStarting;
        }

        // Public Getter ------------------------------------------------------------
        
        public Resources StartingResources { get { return startingResources; } }
        public Dictionary<ResourceType, byte> DefaultLvlResourceRegen { get { return defaultLvlResourceRegen; } }
        public Dictionary<ResourceType, int[]> ResourceRegen { get { return resourceRegen; } }
        public Dictionary<ResourceType, Resources[]> UpgradeCost { get { return upgradeCost; } }

        public int GoldRegenDefault { get { return goldRegenDefault; } }

        /*
       
        public int GoldStarting { get { return goldStarting; } }
        public int WoodStarting { get { return woodStarting; } }
        public int StoneStarting { get { return stoneStarting; } }
        public int MithrilStarting { get { return mithrilStarting; } }
        
        
        
        public int WoodRegen0 { get { return woodRegen0; } }
        public int StoneRegen0 { get { return stoneRegen0; } }
        public int MithrilRegen0 { get { return mithrilRegen0; } }
        void ResourcesRegen0Count()
        {
            resourcesRegen0.Gold = goldRegenDefault;
            resourcesRegen0.Wood = woodRegen0;
            resourcesRegen0.Stone = stoneRegen0;
            resourcesRegen0.Mithril = mithrilRegen0;
        }
        public Resources ResourcesRegen0 { get { return resourcesRegen0; } }

        public int WoodRegen1 { get { return woodRegen1; } }
        public int StoneRegen1 { get { return stoneRegen1; } }
        public int MithrilRegen1 { get { return mithrilRegen1; } }

        public int WoodRegen2 { get { return woodRegen2; } }
        public int StoneRegen2 { get { return stoneRegen2; } }
        public int MithrilRegen2 { get { return mithrilRegen2; } }

        public void Wood2(int thisWood) { thisWood = woodRegen0; }
        */
    }
}
