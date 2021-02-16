using Elements;
using Elements.Geometry;
using Elements.Geometry.Profiles;
using Elements.Geometry.Solids;
using Newtonsoft.Json;
using System;
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
            if (!inputModels.TryGetValue("Floors", out var levelsModel)) { throw new Exception("No floors created. Please create those first."); }

            if (!inputModels.TryGetValue("Envelope", out var envelopesss)) { throw new Exception("No envelopes available. Please make sure MJ_ProceduralMass is outputting envelopes."); }

            var proceduralMassData = envelopesss.AllElementsOfType<ProceduralMassData>().ToArray()[0];

            var proceduralCellSize = proceduralMassData.CellSize;

            //debuggin/ viz things
            List<ModelCurve> coreSketch = new List<ModelCurve>();

            List<SmSpace> placedSpaces = new List<SmSpace>();
            List<SmLevel> _levels = new List<SmLevel>();
            PlacementEngine engine;

            //process levels & floor boundary crvs
            var allFloorProfiles = levelsModel.AllElementsOfType<Floor>().OrderBy(f => f.Elevation).ToList();

            var distinctHeights = allFloorProfiles.Select(s=>s.Elevation).Distinct();

            foreach(var h in distinctHeights)
            {
                 var lvl = new SmLevel(h);
                var boundaries = new List<SmFloorBoundary>();
                foreach(var fl in allFloorProfiles)
                {
                     var sBoundary = new SmFloorBoundary(fl.Profile.Perimeter);
                     if(fl.Elevation == h)
                        boundaries.Add(sBoundary);
                }
                lvl._boundaries = boundaries;
                _levels.Add(lvl);
            }

            ///create unplaced spaces
            List<SmSpace> allUnitsPreplaced = new List<SmSpace>();
            int count = 0;

            for (int i = 0; i < input.UnitMix.Nodes.Count; i++)
            {
                for (int j = 0; j < input.UnitMix.Nodes[i].UnitCount; j++)
                {
                    allUnitsPreplaced.Add(new SmSpace(i, count, false, input.UnitMix.Nodes[i].UnitArea)); //check to see if 'i' corresponds to the right unit type...
                    count++;
                }
            }

            try
            {
                engine = new PlacementEngine(allUnitsPreplaced, (proceduralCellSize - 2.0) * 0.5, _levels, 1.0, input.CorePolygons);

                Console.WriteLine("cell size: " + proceduralCellSize);

                var wallCrvs = engine._Walls.Select(s => new ModelCurve(s._curve)).ToList();

                var coreCrvs = engine.coreLinesViz.Select(s => new ModelCurve(s._curve)).ToList();

                coreSketch.AddRange(coreCrvs);

                string feedbackString = "No feedback yet...";

               

                engine.RunFirstFloor(input.Seam, out feedbackString);

                Console.WriteLine($"Main feedback: {feedbackString}");

               List<string> debugStack;
               engine.TryStackBuilding(out debugStack);

                placedSpaces = engine.PlacedSpaces.ToList();
                Console.WriteLine("rooms should be: " + placedSpaces.Count.ToString());

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }


            var output = new MJProceduralApartmentPlacerOutputs(placedSpaces.Count, allUnitsPreplaced.Count - placedSpaces.Count);

            var materials = new Material[input.UnitMix.Nodes.Count];

            for (int i = 0; i < input.UnitMix.Nodes.Count; i++)
            {
                var col = input.UnitMix.Nodes[i].Color;
                col.Alpha = 1.0;
                materials[i] = new Material(input.UnitMix.Nodes[i].SpaceType, col, 0.0f, 0.0f);
            }

            for (int i = 0; i < placedSpaces.Count; i++)
            {
                var representation = new Representation(new SolidOperation[] { new Extrude(placedSpaces[i].poly.Offset(-0.15)[0], placedSpaces[i].roomLevel._levelHeightToNext-0.25, Vector3.ZAxis, false) });

                var room = new Room(placedSpaces[i].poly.Offset(-0.15)[0], Vector3.ZAxis, $"Unit {placedSpaces[i].roomNumber}", $"{placedSpaces[i].roomNumber}", $"Type {placedSpaces[i].type}", $"{placedSpaces[i].roomNumber}", placedSpaces[i].designArea, 1.0, 0.0, placedSpaces[i].roomLevel._index.ToString(), placedSpaces[i].roomLevel._elevation, placedSpaces[i].roomLevel._levelHeightToNext - 0.25, placedSpaces[i].area, new Transform(0, 0, placedSpaces[i].roomLevel._elevation), materials[placedSpaces[i].type], representation, false, Guid.NewGuid(), "");

                output.Model.AddElement(room);
            }
            output.Model.AddElements(coreSketch);

            return output;
        }
    }
}