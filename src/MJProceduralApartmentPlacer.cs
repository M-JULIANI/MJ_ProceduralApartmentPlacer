using Elements;
using Elements.Geometry;
using Elements.Geometry.Profiles;
using Elements.Geometry.Solids;
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

            // if(!inputModels.TryGetValue("MJ_ProceduralMass", out var cellSizeModel)){throw new Exception("No 'CellSize' received from MJProceduralMassing, please make sure you use that function to create an envelope .");}

            //var cellSize = cellSizeModel.AllElementsOfType<double>().First();

            //debuggin/ viz things
            List<ModelCurve> sketches = new List<ModelCurve>();
            List<ModelCurve> coreSketch = new List<ModelCurve>();
            List<ModelCurve> subSpaceSketch = new List<ModelCurve>();
            List<SmSlivers> sliverSketch = new List<SmSlivers>();
            List<Polygon> outROomSlivs = new List<Polygon>();

            List<SmSpace> placedSpaces = new List<SmSpace>();
            List<SmLevel> _levels = new List<SmLevel>();
            PlacementEngine engine;

            //process levels & floor boundary crvs
            var allFloorProfiles = levelsModel.AllElementsOfType<Floor>().OrderBy(f => f.Elevation).ToList();

            foreach (var f in allFloorProfiles)
            {
                var sBoundary = new SmFloorBoundary(f.Profile.Perimeter);
                var lvl = new SmLevel(f.Elevation, sBoundary);
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

            var listPlaced = new List<SmSpace>();
            try
            {
                //allUnitsPreplaced = SmSpace.Jitter(allUnitsPreplaced, 0.5).ToList();

                engine = new PlacementEngine(allUnitsPreplaced, (input.CellSize - input.CorridorWidth) * 0.5, _levels, 0.5, input.CorePolygons);

                var wallCrvs = engine._Walls.Select(s => new ModelCurve(s._curve)).ToList();

                var coreCrvs = engine.coreLinesViz.Select(s => new ModelCurve(s._curve)).ToList();

                sketches.AddRange(wallCrvs);
                coreSketch.AddRange(coreCrvs);




                string feedbackString = "No feedback yet...";


                engine.RunFirstFloor(input.Seam, out feedbackString);

                for (int i = 0; i < engine._Slivers.Length; i++)
                {
                    foreach (var s in engine._Slivers[i])
                        sliverSketch.Add(s);
                }
                engine.semiSlivers.ToList().ForEach(s => subSpaceSketch.Add(s));
                Console.WriteLine($"Main feedback: {feedbackString}");

                placedSpaces = engine._PlacedProgramSpaces.ToList();
                Console.WriteLine("rooms should be: " + placedSpaces.Count.ToString());


                if (engine.PlacedSpaces != null)
                    listPlaced.AddRange(engine.PlacedSpaces);

                // List<string> debugStack;
                // engine.TryStackBuilding(listPlaced, out debugStack);

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }


            var output = new MJProceduralApartmentPlacerOutputs(listPlaced.Count, allUnitsPreplaced.Count - listPlaced.Count);

            var materials = new Material[input.UnitMix.Nodes.Count];

            for (int i = 0; i < input.UnitMix.Nodes.Count; i++)
            {
                var col = input.UnitMix.Nodes[i].Color;
                col.Alpha = 0.85;
                materials[i] = new Material(input.UnitMix.Nodes[i].SpaceType, col, 0.0f, 0.0f);
            }

            for (int i = 0; i < placedSpaces.Count; i++)
            {
                var representation = new Representation(new SolidOperation[] { new Extrude(placedSpaces[i].poly.Offset(-0.15)[0], 2.0, Vector3.ZAxis, false) });

                var room = new Room(placedSpaces[i].poly.Offset(-0.15)[0], Vector3.ZAxis, $"Unit {placedSpaces[i].roomNumber}", $"{placedSpaces[i].roomNumber}", $"Type {placedSpaces[i].type}", placedSpaces[i].sorter.ToString(), placedSpaces[i].designArea, 1.0, 0.0, "none", placedSpaces[i].poly.Centroid().Z, 2.0, placedSpaces[i].area, new Transform(0, 0, placedSpaces[i].poly.Centroid().Z), materials[placedSpaces[i].type], representation, false, Guid.NewGuid(), "");

                output.Model.AddElement(room);
            }



            // for(int i=0; i< sliverSketch.Count; i++)
            // {
            // var representation = new Representation(new SolidOperation[] { new Extrude(sliverSketch[i]._poly, 2.0, Vector3.ZAxis, false) });

            //   var room = new Room(sliverSketch[i]._poly, Vector3.ZAxis, $"Unit {sliverSketch[i]._stIndex}", $"{sliverSketch[i]._stIndex}", $"Type {sliverSketch[i]._stIndex}", sliverSketch[i]._stIndex.ToString(), 10, 1.0, 0.0, 0, 2.0, 0, new Transform(0,0, 0), materials[0], representation, false, Guid.NewGuid(), "");

            //   output.Model.AddElement(room);
            // }    

            output.Model.AddElements(sketches);
            output.Model.AddElements(coreSketch);
            /// output.Model.AddElements(subSpaceSketch);
            //output.Model.AddElements(sliverSketch);

            // Console.WriteLine("sliver count is: "+ engine._SubSpaces.Length.ToString());



            //  foreach(var s in engine._Slivers[0])
            //   output.Model.AddElement(new ModelCurve(s));




            // var profile = WideFlangeProfileServer.Instance.GetProfileByType(WideFlangeProfileType.W10x100);
            // var pp = 1;
            //             foreach(var p in engine.startPts)
            //             {
            //               output.Model.AddElement(new Column(p, 2.0 * pp, profile, BuiltInMaterials.Steel));
            //               pp++;
            //             }


            return output;
        }
    }
}