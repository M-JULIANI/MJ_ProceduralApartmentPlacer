using Elements;
using Elements.Geometry;
using Elements.Geometry.Solids;
using System;
using System.Collections.Generic;
using System.Linq;
using GeometryEx;
using System.Threading.Tasks;

 namespace MJProceduralApartmentPlacer
{
 
 public class PlacementEngine
  {
    //private List<SmSpace> _PlaceableSpaces;
    public SmWall []  _Walls;


     private List<double> _areas;
     private List<string> _spaces;

     public List<SmWall> coreLinesViz;

    private int _GlobalIndex;

    public List<int> indecesforPurgin;


    Polygon _Core;
    public Polyline _BoundaryCurve;
    Polyline _boundaryPoly;
    public Polygon _SortingCurve;
    public IList<Polygon> _MainFace;
    public Polygon [] _QuadAreas;
    public Polygon [] _SubSpaces;
    public List<Polygon> semiSlivers;
    public List<SmSlivers>[] _Slivers;

    public SmSlivers [] _smSubSpaces;

//previously datatree<polygon>
    public Dictionary<int, List<SmSlivers>> _PlacedProgramSlivers;
    public List<SmSpace> _PlacedProgramSpaces;
    public Dictionary<int, SmSpace> _ProcessedProgram;

    double _SplitInterval;
    double _leaseOffset;
    double medOffset;

    public List<Curve> _Inters = new List<Curve>();
    public List<SmLevel> inLvls;

public List<Vector3> startPts;
    public List<SmSpace> PlacedSpaces;
    public List<SmSpace> _PlaceableSpaces;

    public List<string> _debugger;

    public List<Polygon> coreCrvs;
    public List<Vector3> tPoints;

    private double _worldScale = 1.0;

  public PlacementEngine()
  {

  }
    public PlacementEngine(List<SmSpace> spaces, double leaseDepth, List<SmLevel> levels, IList<Polygon> corePolys, double splitInterval)
    {
      _leaseOffset = leaseDepth * _worldScale;
      medOffset = _leaseOffset * 0.5;
      splitInterval *= _worldScale;

      inLvls = levels;

      coreCrvs = new List<Polygon>();

      foreach(var p in corePolys)
      {
        var c = p;
        coreCrvs.Add(c);
      }

      var firstLevel = inLvls.OrderBy(l => l._elevation).ToList()[0];
      var boundary = firstLevel._boundaries[0].mainPoly;
     // var orientation = boundary.ClosedCurveOrientation(Vector3d.ZAxis);
      var ssspaces = SmSpace.Jitter(spaces, 0.99).ToList();

       _areas = ssspaces.OrderBy(s=>s.sorter).Select(s=>s.designArea).ToList();
       _spaces = ssspaces.OrderBy(s=>s.sorter).Select(s=>s.roomNumber.ToString()).ToList();

       _PlaceableSpaces = ssspaces;
      Polyline tempPoly= boundary.ToPolyline();

     _boundaryPoly = tempPoly;
      _BoundaryCurve = boundary.ToPolyline();

      _Core = InitCoreCrv(boundary);

      _SortingCurve = InitSortingCrv(boundary);


      _SplitInterval = splitInterval;
      _GlobalIndex = 0;

      // var crvs = new List<Polygon>();
      // crvs.Add(new Polygon(_BoundaryCurve.Vertices));
      // crvs.Add(new Polygon(_Core.Vertices));
      _MainFace = boundary.Difference(_Core);

    //InitSpaces();
     InitWalls();

      _PlacedProgramSpaces = new List<SmSpace>();  
     //InitSubSpaces(0.5, corePolys);

    }

    public Polygon InitSortingCrv(Polygon boundarypoly)
    {
      Polygon selectedCrv;
      double midLease = _leaseOffset * 0.5;
      selectedCrv = boundarypoly.Offset(-midLease, EndType.ClosedPolygon)[0];
      return selectedCrv;
    }

    public Polygon InitCoreCrv(Polygon poly)
    {

      Polygon insetOffset = poly.Offset(-_leaseOffset, EndType.ClosedPolygon)[0];
      return insetOffset;
    }



        public void RunFirstFloor(double _seamFactor, out string message)
        {
            message = "None";
            double areaMissing;
            if (CheckOverallArea(out areaMissing) == false)
            {
                //if(!inRevit)
                areaMissing *= 1.0; message = string.Format("Floor plate not large enough, short by {0} sqm", areaMissing);
            }
            else
                message = "All areas fit.";

            {
                InitSubSpaces(_seamFactor, coreCrvs);
                _PlacedProgramSlivers = new Dictionary<int, List<SmSlivers>>();
                //smart slivers ordered by their origigal index
                var stSubs = _smSubSpaces.OrderBy(s => s._shiftIndex).ToList();
                

                for (int i = 0; i < _PlaceableSpaces.Count; i++)
                {
                    string thing = "";
                    TryPlace(_PlaceableSpaces[i], i, stSubs, out thing);
                    Console.WriteLine(i.ToString() + " " + thing);
                }

                List<string> tempNames = new List<string>();
                for (int i = 0; i < _PlacedProgramSlivers.Keys.Count(); i++)
                    tempNames.Add(_spaces[i]);

                semiSlivers = new List<Polygon>();
                for (int i = 0; i < _PlacedProgramSlivers.Keys.Count(); i++)
                {
                  List<SmSlivers> slivs;
                    if (_PlacedProgramSlivers.TryGetValue(i, out slivs))
                    {

                      if(slivs != null && slivs.Count>0)
                      {
                        Console.WriteLine("placed program: " + slivs.Count.ToString());
                        //Polygon unionest;
                        try
                        {
                          // if(i==1)
                          // {
                          // System.IO.File.WriteAllText("D:/Hypar/testUnionAll_2.json", Newtonsoft.Json.JsonConvert.SerializeObject(slivs.OrderBy(s=>s._shiftIndex).Select(s=>s._poly).ToList()));
                        //var polys = slivs.Select(s=>s._poly).ToList();
                        // var profiles = polys.Select(s=>new Profile(s));
                        //var profileResult = Profile.UnionAll(profiles);
                          // }
                        //var rawUnion = Polygon.UnionAll(polys)[0];

                        //if(rawUnion!= null)
                       // {
                        //semiSlivers.Add(rawUnion);
                        foreach(var s in slivs)
                        {
                          var space = new SmSpace(_PlaceableSpaces[i].type, _PlaceableSpaces[i].roomNumber, true, _PlaceableSpaces[i].designArea, s._poly);
                        space.sorter = i;
                        _PlacedProgramSpaces.Add(space);
                        }
                 
                       // }
                        }
                         catch(Exception ex)
                         {
                           Console.WriteLine(ex + slivs.ToString() + "Count: " + slivs.Count);
                         }
                        
                      }
                     // else
                      //{
                     //   Console.WriteLine("slivs: " + slivs.ToString());
                      //}
                    }


                }

                //ProcessPolygons(this._PlacedProgram, tempNames);
            }
        }

    // public void TryStackBuilding(List<SmSpace> units, out List<string> outMess)
    // {
    //   outMess = new List<string>();

    //   var sortedLvls = inLvls.OrderBy(l => l._elevation).ToList();
    //   for (int i = 1; i < sortedLvls.Count; i++)//exclude first level
    //   {
    //     var boundaries = sortedLvls[i]._boundaries;

    //     for (int j = 0; j < boundaries.Count; j++)
    //     {

    //       var mess = TryProject(units, boundaries[j].offsetCrv, boundaries[j].mainCrv, sortedLvls[i]);// ground floor units, various boundaries
    //       outMess.Add(mess.ToString());
    //     }

    //   }

    // }

    // public bool TryProject(List<SmSpace> firstLvlUnits, Curve offCrv, Curve mainCrv, SmLevel level)
    // {
    //   bool worked = false;
    //   var workPlane = new Plane(new Vector3(0, 0, level._elevation), Vector3.ZAxis);

    //   for (int i = 0; i < firstLvlUnits.Count; i++)
    //   {
    //     var dupCrv = firstLvlUnits[i].curve.DuplicateCurve();
    //     var movedCrv = Rhino.Geometry.Curve.ProjectToPlane(dupCrv, workPlane); // projecting ground units to variable levels

    //     Polyline poly;
    //     if(movedCrv.TryGetPolyline(out poly))
    //     {
    //       //worked = true;
    //       var segments = poly.Segments();

    //       var pts = segments.Select(s => s.PointAt(0)).ToList(); // getting unit crv poly pts
    //       string mess;
    //       bool inBool = AllPtsIn(offCrv, pts, out mess);

    //       if(inBool)
    //       {
    //         var unitN = new SmSpace(firstLvlUnits[i].roomNumber, firstLvlUnits[i].area);
    //         unitN.poly = movedCrv;
    //         PlacedSpaces.Add(unitN);
    //         worked = true;
    //       }
    //       else if (inBool == false && mess == "trim")
    //       {
    //         Curve crvOut;
    //         if(TrimKeep(mainCrv, movedCrv, out crvOut, level))
    //         {
    //           var unitN = new SmSpace(firstLvlUnits[i].roomNumber, firstLvlUnits[i].area);
    //           unitN.poly = crvOut;
    //           PlacedSpaces.Add(unitN);
    //           worked = true;
    //         }
    //       }
    //     }
    //     else
    //       continue;
    //   }

    //   return worked;

    // }

    // public bool TrimKeep(Curve trimCrv, Curve toTrim, out Curve crvOut, SmLevel level)
    // {
    //   bool trimmed = false;
    //   var cutC = new List<Curve>();
    //   cutC.Add(trimCrv);
    //   var face = new Polygon(toTrim.ToPolyline().Vertices);
    //   crvOut = toTrim;

    //   var faces = face.Split(cutC, 0.1).ToList();

    //   Plane workPlane = new Plane(new Vector3(0, 0, level._elevation), Vector3.ZAxis);

    //   for (int i = 0; i < faces.Count; i++)
    //   {
    //     if(new Polygon(trimCrv.ToPolyline().Vertices).Contains(faces[i]))
    //     {
    //       var nakedCrvs = faces[i].DuplicateNakedEdgeCurves(true, false);
    //       crvOut = Curve.JoinCurves(nakedCrvs)[0];
    //       return true;
    //     }

    //   }

    //   return trimmed;
    // }


    public bool AllPtsIn(Curve curve, List<Vector3> pts, out string message)
    {
      bool allIn = true;
      message = "clean";

      int countIn = 0;

      Plane workPlane = new Plane(curve.PointAt(0), Vector3.ZAxis);
      for (int i = 0; i < pts.Count; i++)
        if(new Polygon(curve.ToPolyline().Vertices).Contains(pts[i]))
          countIn++;
      if(countIn == 0)
        allIn = false;
      else if(countIn == pts.Count)
        allIn = true;
      else
        allIn = false;

      if(countIn != 0 && countIn != pts.Count)
        message = "trim";

      return allIn;
    }


    public void TryPlace(SmSpace space, int spaceIndex, List<SmSlivers> stSubs, out string report)
    {
      bool Placed = false;
      var areaAccumulated = 0.0;
      report = "Placeholder!";

      double threshold = _leaseOffset *0.5;
      //if(!inRevit)
      threshold *= (_worldScale * _worldScale);

      while(Placed == false)
      {

        Console.WriteLine($"index: {spaceIndex}, areaAccum: {areaAccumulated}");
        if(Math.Abs(areaAccumulated - space.designArea) <= threshold){

          if(indecesforPurgin.Contains(stSubs[_GlobalIndex]._stIndex))
          {
            bool placedExtras = false;

            while(placedExtras == false)
            {
              if(indecesforPurgin.Contains(stSubs[_GlobalIndex]._stIndex) == false){
                placedExtras = true;
                Placed = true;

              }
              else{

                int twIndex = stSubs[_GlobalIndex]._shiftIndex;

                // if dictionary index key exists
                if(_PlacedProgramSlivers.TryGetValue(spaceIndex, out var listSpaces))
                {
                  listSpaces.Add(stSubs[twIndex]);
                  // var newList = new List<Polygon>();
                  // newList.AddRange(listSpaces);
                  // _PlacedProgramSlivers[spaceIndex] = newList;
              
                }
                //if it doesnt...
                else
                {
                  _PlacedProgramSlivers.Add(spaceIndex, new List<SmSlivers>() {stSubs[twIndex]} );
                }
                 areaAccumulated += Math.Abs(stSubs[twIndex]._poly.Area());
                _GlobalIndex++;
              }
            }
          }
          report = stSubs[_GlobalIndex]._shiftIndex.ToString();
          Placed = true;
          break;
        }

        if(_GlobalIndex >= this.semiSlivers.Count - 1)
          break;

        int twIndexy = stSubs[_GlobalIndex]._shiftIndex;
                // if dictionary index key exists
                if (_PlacedProgramSlivers.TryGetValue(spaceIndex, out var _listSpaces))
                {
                    _listSpaces.Add(stSubs[twIndexy]);
                  // var newList = new List<Polygon>();
                  // newList.AddRange(_listSpaces);
                  // _PlacedProgramSlivers[spaceIndex] = newList;

                }
                //if it doesnt...
                else
                {
                    _PlacedProgramSlivers.Add(spaceIndex, new List<SmSlivers>() { stSubs[twIndexy]});
                }
        areaAccumulated += Math.Abs(stSubs[twIndexy]._poly.Area());

        _GlobalIndex++;

      }
    }

    public bool CheckOverallArea(out double areaMissing)
    {
      bool suffArea = true;
      areaMissing = 0;

      double areaRequested = 0.0;
      double localArea = 0.0;
      foreach(var a in _areas){
        localArea = a;
        areaRequested += localArea;

      }

      if(Math.Abs(_MainFace[0].Area()) - Math.Abs(_MainFace[1].Area()) <= areaRequested){
        areaMissing = areaRequested - Math.Abs(_MainFace[0].Area());
        suffArea = false;
      }
      return suffArea;
    }

    bool IntersectsGroupOfLines(Ray ray, Line [] linesToIntersect, out Vector3 firstVec)
    {
      bool hit = false;
      firstVec = Vector3.Origin;

      for(int i=0; i< linesToIntersect.Length; i++)
      {
        Vector3 hitResult;
        if(ray.Intersects(linesToIntersect[i], out hitResult))
        {
          firstVec = hitResult;
         return true;
        }
      }

      return hit;
    }
    public void InitWalls()
    {
      var internalestOffsetDist = 0.25;
      var innerMostCorePolygon = _Core.Offset(-internalestOffsetDist, EndType.ClosedPolygon)[0];
      var initCoreLines = _Core.Segments();

       _Walls = new SmWall[_Core.Segments().Length];

       coreLinesViz = new List<SmWall>();
       for ( int i = 0; i< innerMostCorePolygon.Segments().Length; i++)
       {
         coreLinesViz.Add(new SmWall(i, innerMostCorePolygon.Segments()[i]));
       }

       var _WallA = new SmWall[initCoreLines.Length];
       startPts = new List<Vector3>();
       
        for ( int i = 0; i< innerMostCorePolygon.Segments().Length; i++)
       {

      //   // original curve
        var startCorePt = initCoreLines[i].PointAt(0.0);
         var endCorePt = initCoreLines[i].PointAt(1.0);
         startPts.Add(startCorePt);
          var v1 = initCoreLines[i].End - initCoreLines[i].Start;


          var crossDir = v1.Cross(Vector3.ZAxis).Unitized();
          var crossStart = startCorePt + crossDir * _leaseOffset;
           var crossEnd = endCorePt + crossDir * _leaseOffset;

         Ray coreRay = new Ray(startCorePt, v1);

        Vector3 intersectVec;
         if(IntersectsGroupOfLines(coreRay, innerMostCorePolygon.Segments(), out intersectVec))
         {
           var intersectDist = (intersectVec- startCorePt).Length();
           var origDist = initCoreLines[i].Length();
           if(Math.Abs(origDist- intersectDist)+ internalestOffsetDist < _leaseOffset)
           {
           //use perim logic
           _WallA[i] = new SmWall(i, new Line(crossStart, crossEnd));
           _WallA[i]._flipped = true;
           }
           else
           {
              _WallA[i] = new SmWall(i, initCoreLines[i].ExtendEnd(_leaseOffset));
           }
         }
         else
         {
          _WallA[i] = new SmWall(i, initCoreLines[i].ExtendEnd(_leaseOffset));
        
         }
        // });
        
       }

       _Walls = _WallA;

       
    }

        // public Polygon [] SortGeo(List<Polygon> Polygons)
        // {
        //   SmAreaPt [] initAreas = new SmAreaPt[Polygons.Count];
        //   int[] indices = Enumerable.Range(0, Polygons.Count).ToArray();
        //   System.Threading.Tasks.Parallel.ForEach(indices, (i) => {
        //     var score = ComputeScoreByCurve(Polygons[i], _SortingCurve);
        //     initAreas[i] = new SmAreaPt(Polygons[i], score);
        //     });

        //   var output = initAreas.OrderBy(i => i._score).Select(s => s._poly).ToArray();
        //   return output;
        // }

        public Polygon [] SortGeo(List<SmSlivers> Slivers, Curve curve, bool flipped)
        {
          SmAreaPt [] initAreas = new SmAreaPt[Slivers.Count];
          //int[] indices = Enumerable.Range(0, Polygons.Count).ToArray();
         // System.Threading.Tasks.Parallel.ForEach(indices, (i) => 
          for(int i = 0; i< Slivers.Count ; i++)
          {
            var score = ComputeScoreByCurve(Slivers[i]._poly, curve, flipped);
            initAreas[i] = new SmAreaPt(Slivers[i]._poly, score);
            //});
          }

          var output = initAreas.OrderBy(i => i._score).Select(s => s._poly).ToArray();

          return output;
        }

        public double ComputeScoreByCurve(Polygon polygon, Curve curve, bool flipped)
        {
          var localCentroid = polygon.Centroid();
          var measurePt = curve.PointAt(1.0);
          if(flipped)
             measurePt = curve.PointAt(0.0);
          var dist = measurePt.DistanceTo(localCentroid);
          return dist;
        }

        bool ContainedInCoreCrvs(SmSlivers sliver, List<Polygon> corePolys)
        {
          bool contained = false;

          if(corePolys!= null && corePolys.Count>0)
          {
           for (int c = 0; c < corePolys.Count; c++)
           {
            if(corePolys[c].Contains(sliver._poly.Centroid()))
            {
              return true;
            }
           }
          }

           return contained;
        }

        Line AlignWallPlease(Polygon sortingPoly, SmWall authorityWall)
        {
          Line freshLine = null;
          try
          {
            var closestSeg = sortingPoly.Segments().OrderBy(s=>s.PointAt(0.5).DistanceTo(authorityWall._curve.PointAt(0.5))).ToList()[0];

            Vector3 [] segPts = new Vector3[2];
            segPts[0] = closestSeg.PointAt(0.0);
            segPts[1] = closestSeg.PointAt(1.0);

            var sortedPts = segPts.OrderBy(s=>s.DistanceTo(authorityWall._curve.PointAt(0.0))).ToArray();

            freshLine = new Line(sortedPts[0], sortedPts[1]);
          }
          catch (Exception ex)
          {
            Console.WriteLine(ex);
          }

            return freshLine;
        }

        public void InitSubSpaces(double _seamFactor, IList<Polygon> corePolys)
        {

            _SubSpaces = new Polygon[_Walls.Length];
            semiSlivers = new List<Polygon>();
             indecesforPurgin = new List<int>();
            int pIntOffset = 0;

            double moduleSliver = 0.5;

            double crvLength = 0.0;
            List<Line> splitLines = new List<Line>();
            Line ln;


            //creation of new zones
            for (int w = 0; w < _Walls.Length; w++)
            {
                crvLength = _Walls[w]._curve.Length();
                var numSlivers = (int)Math.Floor(crvLength / moduleSliver);


                ln = new Line(_Walls[w]._curve.PointAt(0.0), _Walls[w]._curve.PointAt(1.0));
                splitLines = ln.DivideByLength(numSlivers, false);

                    var crossDir = Vector3.ZAxis;

                    if (_Walls[w]._flipped == true)
                        crossDir *= -1.0;

                    var diagPoint = ln.Start + _Walls[w]._direction.Cross(crossDir).Unitized() * _leaseOffset + (ln.End - ln.Start);
                    var p = Polygon.Rectangle(ln.Start, diagPoint);
                    _SubSpaces[w] = p;

            }


            var otherSubSpaces = new Polygon[ _Walls.Length];
            for (int i = 0; i < _SubSpaces.Length; i++)
            {
                Polygon resultingPoly = null;

                Polygon [] offsets = new Polygon [1];

                if (i == 0)
                {
                    offsets[0] = _SubSpaces[_SubSpaces.Length - 1];
                    var splitters = _SubSpaces[i].Difference(offsets);

                    if(splitters.Count==2 )
                    {
                       resultingPoly = splitters[1];
                    }
                    else 
                      resultingPoly = splitters[0];

                      otherSubSpaces[i] = resultingPoly;
                }
                else
                {

                  offsets[0] = _SubSpaces[i - 1];
                    var splitters = _SubSpaces[i].Difference(offsets);

                    if(splitters.Count==2)
                    {
                       resultingPoly = splitters[1];
                    }
                    else 
                      resultingPoly = splitters[0];

                       otherSubSpaces[i] = resultingPoly;
                }
            }
            _SubSpaces = otherSubSpaces;

            var tempWalls = new SmWall[_Walls.Length];

            for (int w = 0; w < _Walls.Length; w++)
            {

              var newWallSeg = AlignWallPlease(_SubSpaces[w], _Walls[w]);
              //var closestSeg = _SubSpaces[w].Segments().OrderBy(s=>s.PointAt(0.5).DistanceTo(_Walls[w]._curve.PointAt(0.5))).ToList()[0];


              tempWalls[w] = new SmWall(w, newWallSeg);

              if(_Walls[w]._flipped)
                tempWalls[w]._flipped = true;

                tempWalls[w]._direction = _Walls[w]._direction;
                tempWalls[w]._normalDir = _Walls[w]._normalDir;

            }

           _Walls = tempWalls;




              _Slivers = new List<SmSlivers>[_SubSpaces.Length];
            //creation of SLIVERS
            int counter = 0;
            for (int w = 0; w < _Walls.Length; w++)
            {
                ln = new Line(_Walls[w]._curve.PointAt(0.0), _Walls[w]._curve.PointAt(1.0));
                splitLines = ln.DivideByLength(moduleSliver, false);
                //Console.WriteLine("numSplitLines: " + splitLines.Count.ToString());

                var tempList = new List<SmSlivers>();

                for (int i = 0; i < splitLines.Count; i++)
                {
                    var crossDir = Vector3.ZAxis;

                    if (_Walls[w]._flipped == true)
                        crossDir *= -1.0;

                    var diagPoint = splitLines[i].Start + _Walls[w]._direction.Cross(crossDir).Unitized() * _leaseOffset + (splitLines[i].End - splitLines[i].Start);
                    var p = Polygon.Rectangle(splitLines[i].Start, diagPoint);

                    if(_SubSpaces[counter].Contains(p.Centroid()))
                      tempList.Add(new SmSlivers(i, p));

                }
                _Slivers[counter] = tempList;
                counter++;
            }
 
            
            for (int j = 0; j < _Slivers.Length; j++)
            {
              var PIndexes = new List<int>();

              //ar sortedFaces = SortGeo(_Slivers[j], _Walls[j]._curve, _Walls[j]._flipped).ToList();

              var sortedFaces = _Slivers[j].ToList();

               for (int s = 0; s < sortedFaces.Count; s++)
               {
                 if(ContainedInCoreCrvs(sortedFaces[s], coreCrvs)){
                    sortedFaces.RemoveAt(s);
                    s--;
                }
               }

              var sfCount = sortedFaces.Count;

              for (int s = 6; s >= 0; s--)
              {
                var subIndex = sfCount - 1 - s;
                var subModIndex = pIntOffset + subIndex;
                if(!PIndexes.Contains(subModIndex))
                  PIndexes.Add(subModIndex);
              }


              semiSlivers.AddRange(sortedFaces.OrderBy(s=>s._stIndex).Select(s=>s._poly));
             indecesforPurgin.AddRange(PIndexes);
              pIntOffset += sfCount;
            }
            

            //Init smart slivers
            _smSubSpaces = new SmSlivers[semiSlivers.Count];
            for (int g = 0; g < _smSubSpaces.Length; g++)
              _smSubSpaces[g] = new SmSlivers(g, semiSlivers[g]);

            int modIndex = 0;
            int indexOffset = (int) (_seamFactor * (semiSlivers.Count));
            var modSubSpaces = new Polygon[semiSlivers.Count];

            var lastIndexContainer = new List<int>();

            for (int i = 0; i < semiSlivers.Count; i++)
            {
              modIndex = (i + indexOffset) % semiSlivers.Count;
              modSubSpaces[i] = semiSlivers[modIndex];
              _smSubSpaces[i]._shiftIndex = modIndex;
            }

            semiSlivers = modSubSpaces.ToList();
        }

    public void ProcessPolygons(Dictionary<int, List<SmSlivers>> PolygonTree, List<string> spaceNames)
    {

      _ProcessedProgram = new Dictionary<int, SmSpace>();

      SmSpace [] initSpaces = new SmSpace[PolygonTree.Keys.Count()];
     // int[] indices = Enumerable.Range(0, initSpaces.Length).ToArray();
      //System.Threading.Tasks.Parallel.ForEach(indices, (index) => 
      for (int i = 0; i < PolygonTree.Keys.Count; i++)
      {
        if(PolygonTree.TryGetValue(i, out var branchSpaces))
        {
        var branchPolygons = branchSpaces.Select(s=>s._poly).ToList();;
        var mergedPolygon = Polygon.UnionAll(branchPolygons)[0];
        initSpaces[i] = new SmSpace(int.Parse(spaceNames[i]), Math.Abs(mergedPolygon.Area()));
        initSpaces[i].poly = mergedPolygon;

        _ProcessedProgram.Add(i, initSpaces[i]);
        }
      }
       // });

      PlacedSpaces = new List<SmSpace>();
      PlacedSpaces.AddRange(initSpaces.OrderBy(o => o.roomNumber).ToList());
    }
  }
}