using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NemesesGame
{
    public struct Resources
    {
        int gold;
        int wood;
        int stone;
        int mithril;

        public int Gold { get { return gold; } set { gold = value; } }
        public int Wood { get { return wood; } set { wood = value; } }
        public int Stone { get { return stone; } set { stone = value; } }
        public int Mithril { get { return mithril; } set { mithril = value; } }

        public Resources(int thisGold, int thisWood, int thisStone, int thisMithril)
        {
            gold = thisGold;
            wood = thisWood;
            stone = thisStone;
            mithril = thisMithril;
        }

        public static Resources operator +(Resources a, Resources b)
        {
            Resources c = new Resources();
            c.gold = a.gold + b.gold;
            c.wood = a.wood + b.wood;
            c.stone = a.stone + b.stone;
            c.mithril = a.mithril + b.mithril;

            return c;
        }

        public static Resources operator -(Resources a, Resources b)
        {
            Resources c = new Resources();
            c.gold = a.gold - b.gold;
            c.wood = a.wood - b.wood;
            c.stone = a.stone - b.stone;
            c.mithril = a.mithril - b.mithril;

            return c;
        }
    }
}
