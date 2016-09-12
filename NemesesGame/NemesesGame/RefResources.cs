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
        /*
        List<int> woodRegenLvl;
        List<int> stoneRegenLvl;
        List<int> mithrilRegenLvl;
        */
        
        // Starting Resources
        int goldStarting = 2000;
        int woodStarting = 250;
        int stoneStarting = 100;
        int mithrilStarting = 50;
        Resources startingResources;

        // Resource Production Rate : 1
        int goldRegenDefault = 2000;
        int woodRegen0 = 50;
        int stoneRegen0 = 20;
        int mithrilRegen0 = 0;
        //Resources resourcesRegen0;

        // Resource Production Rate : 2
        int woodRegen1 = 100;
        int stoneRegen1 = 60;
        int mithrilRegen1 = 30;

        // Resource Production Rate : 3
        int woodRegen2 = 300;
        int stoneRegen2 = 120;
        int mithrilRegen2 = 100;

        void InitRefDict ()
        {
            defaultLvlResourceRegen.Add(ResourceType.Wood, 0);
            defaultLvlResourceRegen.Add(ResourceType.Stone, 0);
            defaultLvlResourceRegen.Add(ResourceType.Mithril, 0);

            resourceRegen.Add(ResourceType.Wood, new int[] { woodRegen0, woodRegen1, woodRegen2 });
            resourceRegen.Add(ResourceType.Stone, new int[] { stoneRegen0, stoneRegen1, stoneRegen2 });
            resourceRegen.Add(ResourceType.Mithril, new int[] { mithrilRegen0, mithrilRegen1, mithrilRegen2 });
            /*
            woodRegenLvl.Add(woodRegen0);
            woodRegenLvl.Insert(0, woodRegen0);
            woodRegenLvl.Insert(1, woodRegen1);
            woodRegenLvl.Insert(2, woodRegen2);

            stoneRegenLvl.Insert(0, stoneRegen0);
            stoneRegenLvl.Insert(1, stoneRegen1);
            stoneRegenLvl.Insert(2, stoneRegen2);

            mithrilRegenLvl.Insert(0, mithrilRegen0);
            mithrilRegenLvl.Insert(1, mithrilRegen1);
            mithrilRegenLvl.Insert(2, mithrilRegen2);
            */
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

        public int GoldRegenDefault { get { return goldRegenDefault; } }

        /*
        public List<int> WoodRegenLvl { get { return woodRegenLvl; } }
        public List<int> StoneRegenLvl { get { return stoneRegenLvl; } }
        public List<int> MithrilRegenLvl { get { return mithrilRegenLvl; } }


        
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
