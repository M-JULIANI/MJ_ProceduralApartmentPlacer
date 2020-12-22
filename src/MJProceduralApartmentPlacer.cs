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
            var output = new MJProceduralApartmentPlacerOutputs();
            return output;
        }
      }
}