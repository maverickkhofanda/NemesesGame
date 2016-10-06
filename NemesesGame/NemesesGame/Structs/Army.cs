using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NemesesGame;

namespace NemesesGame
{
    //public enum ArmyType { Regulars }
    /// <summary>
    /// Current army action
    /// </summary>
    public enum ArmyState { Base, March /*,Target ?*/, Invade, Intercept }
    
    public class Army
    {   
        public Army ()
        {
            // Only use fronts[0] for army in Base
            fronts[0] = new ArmyFront(ArmyState.Base, startingNumber);
            //fronts.Add(new ArmyFront(ArmyState.Base, startingNumber));

            //init typeNumber dict
            //typeNumber.Add(ArmyType.Regulars, 1000);

            //init cost
            //cost.Add(ArmyType.Regulars, regularsCost);
        }
        ArmyFront[] fronts = new ArmyFront[10]; //thus, each cities can only have 10 fronts
        //List<ArmyFront> fronts = new List<ArmyFront>();

        int startingNumber = 1000;
        float power = 1.0f;

        int cost = 3;
        //float costX = 1.0f; // cost multiplier don't use this yet...

        byte invadeMarchLength = 3;
		byte interceptMarchLength = 1;

		public void StartMarch(byte thisFrontId, long targetId, byte frontId)
        {
            Fronts[thisFrontId].TargetTelegramId = targetId;
            Fronts[thisFrontId].TargetFrontId = frontId;
            Fronts[thisFrontId].State = ArmyState.March;
			// If the target is base, set march length to invadeMarchLength
			// Else (target is marching army, intercepting), set march length to interceptMarchLength
			if (frontId == 0)
				Fronts[thisFrontId].MarchLeft = invadeMarchLength;
			else
				Fronts[thisFrontId].MarchLeft = interceptMarchLength;

			//Console.WriteLine("targetId= " + targetId);
			//Console.WriteLine("Fronts[thisFrontId] TargetId = " + Fronts[thisFrontId].TargetTelegramId);
			//Console.WriteLine("Fronts[thisFrontId] TargetId = " + Fronts[thisFrontId].TargetTelegramId);
			//Console.WriteLine("TargetFrontId = " + Fronts[thisFrontId].TargetFrontId);
			//Console.WriteLine("armyState = " + Fronts[thisFrontId].State);


		}

        public ArmyFront[] Fronts { get { return fronts; } set { fronts = value; } }
        //public List<ArmyFront> Fronts { get { return fronts; } set { fronts = value; } }
        public float Power { get { return power; } set { power = value; } }
        public int Cost { get { return cost; } set { cost = value; } }




        // ----------------------------------------------------------------------
        //Dictionary<ArmyState,  int> fronts = new Dictionary<ArmyState, int>();
        //Dictionary<ArmyType, int> typeNumber = new Dictionary<ArmyType, int>();

        //Dictionary<ArmyType, int> cost = new Dictionary<ArmyType, int>(); // for now, we will only use gold

        //public int Regulars { get { return typeNumber[ArmyType.Regulars]; } set { typeNumber[ArmyType.Regulars] = value; } }

        //public Dictionary<ArmyState, Dictionary<ArmyType, int>> Fronts { get { return fronts; } set { fronts = value; } }
        //public Dictionary<ArmyType, int> TypeNumber { get { return typeNumber; } set { typeNumber = value; } }

        //public Dictionary<ArmyType, int> Cost { get { return cost; } set { cost = value; } }
    }
}
