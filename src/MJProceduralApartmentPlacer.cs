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
          if(!inputModels.TryGetValue("Floors", out var levelsModel)){throw new Exception("No floors created. Please create those first.");}

          Console.WriteLine("nothing seems to be working");
          // if(!inputModels.TryGetValue("MJ_ProceduralMass", out var cellSizeModel)){throw new Exception("No 'CellSize' received from MJProceduralMassing, please make sure you use that function to create an envelope .");}

          //var cellSize = cellSizeModel.AllElementsOfType<double>().First();

          List<ModelCurve> sketches = new List<ModelCurve>();

          List<SmLevel> _levels= new List<SmLevel>();
          PlacementEngine engine;

          //process levels & floor boundary crvs
          var allFloorProfiles = levelsModel.AllElementsOfType<Floor>().OrderBy(f=>f.Elevation).ToList();

          foreach(var f in allFloorProfiles)
          {
            var sBoundary = new SmFloorBoundary(f.Profile.Perimeter);
            var lvl = new SmLevel(f.Elevation, sBoundary);
            _levels.Add(lvl);
          }

          ///create unplaced spaces
          List<SmSpace> allUnitsPreplaced = new List<SmSpace>();
          int count = 0;

          for(int i=0; i< input.UnitMix.Nodes.Count; i++)
          {
               for(int j=0; j< input.UnitMix.Nodes[i].UnitCount; j++)
               {
                 allUnitsPreplaced.Add(new SmSpace(i, count, false, input.UnitMix.Nodes[i].UnitArea)); //check to see if 'i' corresponds to the right unit type...
                 count++;
               }
          }

          var listPlaced = new List<SmSpace>();
         // try
         // {
            engine = new PlacementEngine(allUnitsPreplaced.OrderBy(u=>u.roomNumber).ToList(), (input.CellSize - input.CorridorWidth) * 0.5,_levels, input.CorePolygons, 0.5);

            var wallCrvs = engine._Walls.Select(s=>new ModelCurve(s._curve)).ToList();

            Console.WriteLine("WALL COUNT IS: " + wallCrvs.Count.ToString());
            sketches.AddRange(wallCrvs);

            

            //string feedbackString = "No feedback yet...";

          //  engine.RunFirstFloor(input.Seam, out feedbackString);

            if(engine.PlacedSpaces!= null)
              listPlaced.AddRange(engine.PlacedSpaces);

            // List<string> debugStack;
            // engine.TryStackBuilding(listPlaced, out debugStack);

         // }
        //  catch (Exception e)
         // {
         //   Console.WriteLine(e.ToString());
        //  }


            var output = new MJProceduralApartmentPlacerOutputs(listPlaced.Count, allUnitsPreplaced.Count - listPlaced.Count);

             var tempMat = new Material("envelope", new Color(0.27, 0.73, 0.73, 0.6), 0.0f, 0.0f);

            foreach(var pl in listPlaced)
            {
               var representation = new Representation(new SolidOperation[] { new Extrude(pl.poly, pl.roomHeight, Vector3.ZAxis, false) });
               
              var room = new Room(pl.poly, Vector3.ZAxis, $"Unit {pl.roomNumber}", pl.roomNumber.ToString(), pl.type.ToString(), pl.roomNumber.ToString(), pl.area, 0.0, 0.0, pl.poly.Centroid().Z, pl.roomHeight, pl.poly.Area(), new Transform(0,0, pl.roomHeight), tempMat, representation, false, Guid.NewGuid(), "");

              output.Model.AddElement(room);
            }

            output.Model.AddElements(sketches);

            var pink = new Material("pink", Colors.Pink);

            var profile = WideFlangeProfileServer.Instance.GetProfileByType(WideFlangeProfileType.W10x100);
            var pp = 0;
                        foreach(var p in engine.startPts)
                        {
                          if(pp!=0)
                            break;
                          output.Model.AddElement(new Column(p, 3.0, profile, BuiltInMaterials.Steel));
                          pp++;
                        }


            return output;
        }
      }


    
}