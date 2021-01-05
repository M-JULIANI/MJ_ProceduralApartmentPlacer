using Elements;
using Elements.Geometry;
using System.Collections.Generic;

namespace MJProceduralApartmentPlacer
{
      public static class MJProceduralApartmentPlacer
    {
        /// <summary>
        /// Places an apartment mix in a procedurally generated mass.
        /// </summary>
        /// <param name="model">The input model.</param>
        /// <param name="input">The arguments to the execution.</param>
        /// <returns>A MJProceduralApartmentPlacerOutputs instance containing computed results and the model with any new elements.</returns>
        public static MJProceduralApartmentPlacerOutputs Execute(Dictionary<string, Model> inputModels, MJProceduralApartmentPlacerInputs input)
        {

          List<sSpace> allUnits = new List<sSpace>();
          int count = 0;

          for(int i=0; i< input.UnitMix.Nodes.Count; i++)
          {
               for(int j=0; j< input.UnitMix.Nodes[j].UnitCount; j++)
               {
                 allUnits.Add(new sSpace(i, count, false, input.UnitMix.Nodes[j].UnitArea)); //check to see if 'i' corresponds to the right unit type...
                 count++;
               }
          }
            var output = new MJProceduralApartmentPlacerOutputs();
            return output;
        }
      }


      public class sSpace
      {
        public int type {get; set;} //0, 1, 2
        public double area {get; set;}
        public int roomNumber {get; set;} //room 1, 2, 3

        public bool placed {get; set;}

        public sSpace()
        {
          type = -1;
          roomNumber = -1;
          placed = false;
          area = -1;
        }

        public sSpace(int type, int roomNumber, bool placed, double area)
        {
          this.type = type;
          this.roomNumber = roomNumber;
          this.placed = placed;
          this.area = area;
        }
      }
}