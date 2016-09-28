using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NemesesGame
{
    public class MerchantGlobal
    {
        public MerchantGlobal (byte totalPlayer)
        {
            InitRefPrice();

            InitMisc();


            // set the demand-supply
            // set the constant: the higher the constant, the slower the demand/supply follows the market
            avgWeightConst = 0.8f; // comfortable range: 0.6f - o.9f
            //avgWeightConst = 0.6 + ((0.02 - (0.001 * totalPlayer)) * totalPlayer);

            InitMerchant(totalPlayer);
            
        }
        void InitRefPrice()
        {
            basePrice.Add(ResourceType.Wood, 8);
            basePrice.Add(ResourceType.Stone, 30);
            basePrice.Add(ResourceType.Mithril, 100);

            // priceSpread indicates how far the price can be from the basePrice
            priceSpread.Add(ResourceType.Wood, 1);
            priceSpread.Add(ResourceType.Stone, 4);
            priceSpread.Add(ResourceType.Mithril, 20);

            // buySellSpread shows the HALF of the buy-sell spread
            buySellSpread.Add(ResourceType.Wood, 0.5f);
            buySellSpread.Add(ResourceType.Stone, 1.5f);
            buySellSpread.Add(ResourceType.Mithril, 2.5f);
        }

        Merchant[] merchants;
        ResourceType[] materialTypes = { ResourceType.Wood, ResourceType.Stone, ResourceType.Mithril };

        Dictionary<ResourceType, int> buyPrice = new Dictionary<ResourceType, int>();
        Dictionary<ResourceType, int> sellPrice = new Dictionary<ResourceType, int>();
        Dictionary<ResourceType, float> midPrice = new Dictionary<ResourceType, float>();

        // reference
        Dictionary<ResourceType, int> basePrice = new Dictionary<ResourceType, int>();
        Dictionary<ResourceType, int> priceSpread = new Dictionary<ResourceType, int>();
        Dictionary<ResourceType, float> buySellSpread = new Dictionary<ResourceType, float>();

        // this turn Demand/Supply
        Dictionary<ResourceType, int> thisTurnDemand = new Dictionary<ResourceType, int>();
        Dictionary<ResourceType, int> thisTurnSupply = new Dictionary<ResourceType, int>();

        // demand-supply-const
        Dictionary<ResourceType, float> denominator = new Dictionary<ResourceType, float>();
        Dictionary<ResourceType, float> demSupMult = new Dictionary<ResourceType, float>();
        //Dictionary<ResourceType, float> demandMA;
        //Dictionary<ResourceType, float> supplyMA;
        float avgWeightConst;

        public Dictionary<ResourceType, int> BuyPrice { get { return buyPrice; } set { buyPrice = value; } }
        public Dictionary<ResourceType, int> SellPrice { get { return sellPrice; } set { sellPrice = value; } }
        public Dictionary<ResourceType, float> MidPrice { get { return midPrice; } set { midPrice = value; } }

        public Dictionary<ResourceType, int> ThisTurnDemand { get { return thisTurnDemand; } set { thisTurnDemand = value; } }
        public Dictionary<ResourceType, int> ThisTurnSupply { get { return thisTurnSupply; } set { thisTurnSupply = value; } }

        public Dictionary<ResourceType, int> BasePrice { get { return basePrice; } set { basePrice = value; } }
        public Dictionary<ResourceType, int> PriceSpread { get { return priceSpread; } set { priceSpread = value; } }
        public Dictionary<ResourceType, float> BuySellSpread { get { return buySellSpread; } set { buySellSpread = value; } }

        public Dictionary<ResourceType, float> Denominator { get { return denominator; } set { denominator = value; } }
        public Dictionary<ResourceType, float> DemSupMult { get { return demSupMult; } set { demSupMult = value; } }
        //public Dictionary<ResourceType, float> DemandMA { get { return demandMA; } set { demandMA = value; } }
        //public Dictionary<ResourceType, float> SupplyMA { get { return supplyMA; } set { supplyMA = value; } }
        public float AvgWeightConst { get { return avgWeightConst; } set { avgWeightConst = value; } }

        void InitMisc()
        {
            foreach (ResourceType r in materialTypes)
            {
                denominator.Add(r, 0);
                thisTurnDemand.Add(r, 0);
                thisTurnSupply.Add(r, 0);
            }
        }

        void InitMerchant(byte totalPlayer)
        {
            int nMerchant = totalPlayer / 4;
            merchants = new Merchant[nMerchant];

            // sets the merchant's position
            // not so fair yet...
            Random r = new Random();
            int position = r.Next(0, 2);
            for (int i = 0; i < nMerchant; i++)
            {
                merchants[i].Position = (byte) position;
                Console.WriteLine("Merchants[{0}] position: {1}", i, position);
                position += 4;
            }
        }

        
    }
}
