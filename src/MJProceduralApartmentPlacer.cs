using Elements;
using Elements.Geometry;
using System.Collections.Generic;
using System.Linq;

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
          List<SmLevel> _levels= new List<SmLevel>();
          //process levels & floor boundary crvs
          var floorsModel = inputModels["Floors"];
          var allFloors = floorsModel.AllElementsOfType<Floor>().ToList();
          ///create unplaced spaces
          List<SmSpace> allUnitsPreplaced = new List<SmSpace>();
          int count = 0;

          for(int i=0; i< input.UnitMix.Nodes.Count; i++)
          {
               for(int j=0; j< input.UnitMix.Nodes[j].UnitCount; j++)
               {
                 allUnitsPreplaced.Add(new SmSpace(i, count, false, input.UnitMix.Nodes[j].UnitArea)); //check to see if 'i' corresponds to the right unit type...
                 count++;
               }
          }


            var output = new MJProceduralApartmentPlacerOutputs();
            return output;
        }
      }


    
}