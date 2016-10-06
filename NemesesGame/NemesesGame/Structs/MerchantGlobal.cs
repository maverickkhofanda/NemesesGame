using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NemesesGame
{
    public class MerchantGlobal
    {
        public MerchantGlobal (long[] playerIdArray)
        {
            InitRefPrice();

            InitMisc();

            // set the demand-supply
            avgWeightConst = 0.8f; // comfortable range: 0.6f - o.9f
            //avgWeightConst = 0.6 + ((0.02 - (0.001 * totalPlayer)) * totalPlayer);

            InitMerchant(playerIdArray);
            
        }
        

        Merchant[] merchants;
        ResourceType[] materialTypes = { ResourceType.Wood, ResourceType.Stone, ResourceType.Mithril };
        OrderedDictionary isMerchantInCity = new OrderedDictionary();
        //long[] playerOrder; // this sets the merchant journey track
        //Dictionary<long, bool> isMerchantHere = new Dictionary<long, bool>();

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

        public void UpdateDemandSupply()
        {
            // Denominator setter
            foreach (ResourceType r in materialTypes)
            {
                // Denominator setter
                // sets the first denominator
                if (Denominator[r] == 0)
                {
                    // check if supply/demand still 0
                    if (ThisTurnDemand[r] == 0 && ThisTurnSupply[r] == 0) { }

                    // find the highest number... between supply / demand
                    else if (ThisTurnDemand[r] == 0)
                    {
                        Denominator[r] = ThisTurnSupply[r] / (1 + AvgWeightConst);
                    }
                    else if (ThisTurnSupply[r] == 0)
                    {
                        Denominator[r] = ThisTurnDemand[r] / (1 + AvgWeightConst);
                    }
                    else
                    {
                        Denominator[r] = ThisTurnDemand[r] >= ThisTurnSupply[r] ?
                            ThisTurnDemand[r] :
                            ThisTurnSupply[r];
                    }
                }
                // sets the next denominators
                else
                {
                    // keep the denominator if no transaction
                    if (ThisTurnDemand[r] == 0 && ThisTurnSupply[r] == 0) { }
                    else if (ThisTurnDemand[r] == 0)
                    {
                        Denominator[r] = (ThisTurnSupply[r] + (AvgWeightConst * Denominator[r]))
                                            / (1 + AvgWeightConst);
                    }
                    else if (ThisTurnSupply[r] == 0)
                    {
                        Denominator[r] = (ThisTurnDemand[r] + (AvgWeightConst * Denominator[r]))
                                            / (1 + AvgWeightConst);
                    }
                    else
                    {
                        float f = ThisTurnDemand[r] >= ThisTurnSupply[r] ?
                            ThisTurnSupply[r] :
                            ThisTurnSupply[r];

                        Denominator[r] = (f + (AvgWeightConst * Denominator[r]))
                                            / (1 + AvgWeightConst);
                    }
                }

                // DemandSupplyMultiplier (DemSupMult) setter
                if (Denominator[r] != 0)
                {
                    DemSupMult[r] = ((ThisTurnDemand[r] - ThisTurnSupply[r]) / Denominator[r])
                                    + (DemSupMult[r] * AvgWeightConst);
                }
                else
                {
                    DemSupMult[r] = 0;
                }

                //Console.WriteLine("{0}: DemSupMult = {1}\n{0}: MidPrice = {2}", Enum.GetName(typeof(ResourceType), r), DemSupMult[r], MidPrice[r]);
                // MidPrice setter
                MidPrice[r] = BasePrice[r] + (DemSupMult[r] * PriceSpread[r]);

                //BuyPrice & SellPrice setter
                BuyPrice[r] = (int)Math.Round(MidPrice[r] + BuySellSpread[r], MidpointRounding.AwayFromZero);
                SellPrice[r] = (int)Math.Round(MidPrice[r] - BuySellSpread[r], MidpointRounding.AwayFromZero);

                Console.WriteLine("This turn {0} supply/demand: {1}/{2}", Enum.GetName(typeof(ResourceType), r), ThisTurnSupply[r], ThisTurnDemand[r]);
                Console.WriteLine("Price {0}: {1} | {2}\n", Enum.GetName(typeof(ResourceType), r), BuyPrice[r], SellPrice[r]);
            }

            /* Unused code...
            if (turn == 2)
            {
                foreach (ResourceType r in materialTypes)
                {
                    //Demand[r] = ThisTurnDemand[r];
                    //Supply[r] = ThisTurnSupply[r];
                }
            }
            else
            {
                foreach (ResourceType r in materialTypes)
                {
                    //Demand[r] = (ThisTurnDemand[r] + (AvgWeightConst * Demand[r])) / (1 + AvgWeightConst);
                    //Supply[r] = (ThisTurnSupply[r] + (AvgWeightConst * Supply[r])) / (1 + AvgWeightConst);
                }
            }
            
            foreach (ResourceType r in materialTypes)
            {
                // if Demand is higher, the Pct is +... and vice versa
                DemSupMult[r] = Demand[r] >= SupplyMA[r] ?
                    (Demand[r] - SupplyMA[r]) / Demand[r] :
                    (Demand[r] - SupplyMA[r]) / SupplyMA[r];
            }
            */

            // resets thisTurnDemand/Supply
            foreach (ResourceType r in materialTypes)
            {
                ThisTurnDemand[r] = 0;
                ThisTurnSupply[r] = 0;
            }
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
        void InitMisc()
        {
            foreach (ResourceType r in materialTypes)
            {
                midPrice.Add(r, basePrice[r]);
                buyPrice.Add(r, 
                    (int) Math.Round(midPrice[r] + buySellSpread[r], MidpointRounding.AwayFromZero));
                sellPrice.Add(r, 
                    (int) Math.Round(midPrice[r] - buySellSpread[r], MidpointRounding.AwayFromZero));

                denominator.Add(r, 0);
                demSupMult.Add(r, 0);

                thisTurnDemand.Add(r, 0);
                thisTurnSupply.Add(r, 0);
            }
        }

        void InitMerchant(long[] playerIdArray)
        {
            int nMerchant = (playerIdArray.Count() / 4) + 1;
            int mod = playerIdArray.Count() % 4;

            merchants = new Merchant[nMerchant];
            
            // give numbering to players (use index)
            Program.Shuffle(new Random(), playerIdArray);


            // try using Ordered Dictionary
            // init OrderedDictionary's playerId
            for (int i = 0; i < playerIdArray.Count(); i++)
            {
                isMerchantInCity.Add(playerIdArray[i], false);

                // use the code below to get playerId
                //long pId = (long) isMerchantInCity.Cast<DictionaryEntry>().ElementAt(i).Key;
            }
            
            // init & set the merchant's position
            // still not perfectly fair at large number: the merchant space is 3-3-3-4-4-4-4-4-4 instead of 3-4-4-4-3-4-4-4
            byte pos = 0;
            for (byte i = 0; i < merchants.Count(); i++)
            {
                isMerchantInCity[pos] = true;

                if (mod < 3 && mod != 0)
                {
                    pos += 3;
                    mod++;
                }
                else
                {
                    pos += 4;
                }

                //Console.WriteLine("Merchant[{0}].Position = {1}", i, merchants[i].Position);
            }
            // end Ordered Dictionary



            /*
            playerOrder = playerIdArray;

            // init isMerchant here dict<long, bool>
            foreach (long pId in playerOrder)
            {
                isMerchantHere.Add(pId, false);
            }

            // init & set the merchant's position
            byte pos = 0;
            for (byte i = 0; i < merchants.Count(); i++)
            {
                merchants[i] = new Merchant();

                merchants[i].Position = pos;
                if (mod < 3 && mod != 0)
                {
                    pos += 3;
                }
                else
                {
                    pos += 4;
                }

                //Console.WriteLine("Merchant[{0}].Position = {1}", i, merchants[i].Position);
            }

            */

        }

        void NextPosition ()
        {
            bool isMerchantAtCity0 = false;

            // iterated inversely
            for (int i = isMerchantInCity.Count - 1; i >= 0; i--)
            {
                if ((bool) isMerchantInCity[i] == true)
                {
                    isMerchantInCity[i] = false;

                    // sets next
                    if (i + 1 < isMerchantInCity.Count)
                        isMerchantInCity[i + 1] = true;
                    else
                        isMerchantAtCity0 = true;
                }
            }

            if (isMerchantAtCity0 == true)
                isMerchantInCity[0] = true;
        }

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

        //public Dictionary<long, bool> IsMerchantHere { get { return isMerchantHere; } set { isMerchantHere = value; } }
        public OrderedDictionary IsMerchantInCity { get { return isMerchantInCity; } set { isMerchantInCity = value; } }
    }
}
