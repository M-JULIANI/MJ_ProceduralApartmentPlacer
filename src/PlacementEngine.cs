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
    public List<SmWall> _Walls;


     private List<double> _areas;
     private List<string> _spaces;

    private int _GlobalIndex;

    public List<int> indecesforPurgin;


    Polygon _Core;
    public Polyline _BoundaryCurve;
    Polyline _boundaryPoly;
    public Polygon _SortingCurve;
    public Polygon _MainFace;
    public Polygon [] _QuadAreas;
    public List<Polygon> _SubSpaces;

    public SmSlivers [] _smSubSpaces;

//previously datatree<polygon>
    public Dictionary<int, List<SmSlivers>> _PlacedProgram;
    public Dictionary<int, SmSpace> _ProcessedProgram;

    double _SplitInterval;
    double _leaseOffset;
    double medOffset;

    public List<Curve> _Inters = new List<Curve>();
    public List<string> outputString = new List<string>();
    public Mesh inCoreM;
    public List<SmLevel> inLvls;

    public List<SmSpace> PlacedSpaces;

    public List<string> _debugger;

    public List<Curve> coreCrvs;
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

      coreCrvs = new List<Curve>();

      foreach(var p in corePolys)
      {
        var c = p;
        coreCrvs.Add(c);
      }

      var firstLevel = inLvls.OrderBy(l => l._elevation).ToList()[0];
      var boundary = firstLevel._boundaries[0].mainPoly;
     // var orientation = boundary.ClosedCurveOrientation(Vector3d.ZAxis);

   

      //boundary.

      // if(orientation != CurveOrientation.Clockwise)
      //   boundary.Reverse();

       _areas = spaces.OrderBy(s=>s.roomNumber).Select(s=>s.area).ToList();
       _spaces = spaces.OrderBy(s=>s.roomNumber).Select(s=>s.roomNumber.ToString()).ToList();

      Polyline tempPoly= boundary.ToPolyline();

     _boundaryPoly = tempPoly;
      _BoundaryCurve = boundary.ToPolyline();

      _Core = InitCoreCrv(boundary);


     // var off = boundary.Offset(-leaseDepth, EndType.ClosedPolygon)[0];

  

    

      _SortingCurve = InitSortingCrv(boundary);

      // if(_BoundaryCurve.Segments().Length != _SortingCurve.Segments().Length)
      //   _SortingCurve = _BoundaryCurve;
      
      // double tClosest;
      // _SortingCurve.ClosestPoint(_BoundaryCurve.PointAt(0.0), out tClosest);
      // _SortingCurve.ChangeClosedCurveSeam(tClosest);
      // ///

      // if(_SortingCurve.ClosedCurveOrientation(Vector3d.ZAxis) != _BoundaryCurve.ClosedCurveOrientation(Vector3d.ZAxis))
      //   _SortingCurve.Reverse();

      _SplitInterval = splitInterval;
      _GlobalIndex = 0;

      var crvs = new List<Polygon>();
      crvs.Add(new Polygon(_BoundaryCurve.Vertices));
      crvs.Add(new Polygon(_Core.Vertices));
      _MainFace = Polygon.UnionAll(crvs)[0];

      //InitSpaces();
     InitWalls();

    




    }

    public Polygon InitSortingCrv(Polygon boundarypoly)
    {
      Polygon selectedCrv;

      double midLease = _leaseOffset * 0.5;
      Polygon [] offsetCrvs = new Polygon [2];
      offsetCrvs[0] = boundarypoly.ToPolyline().Offset(-midLease, EndType.ClosedPolygon)[0];
      offsetCrvs[1] = boundarypoly.ToPolyline().Offset(midLease, EndType.ClosedPolygon)[0];


      selectedCrv = offsetCrvs.OrderBy(o => o.Length()).ToList()[0];
      return selectedCrv;
    }

    public Polygon InitCoreCrv(Polygon poly)
    {

     // Plane workPlane = new Plane(_boundaryPoly.PointAt(0.0), Vector3.ZAxis);
      Polygon insetOffset = poly.Offset(-_leaseOffset, EndType.ClosedPolygon)[0];

      // if(polyOut.ToNurbsCurve().ClosedCurveOrientation(Vector3d.ZAxis) != _BoundaryCurve.ClosedCurveOrientation(Vector3d.ZAxis))
      //   polyOut.Reverse();

      return insetOffset;
    }

    // public void RunFirstFloor(double _seamFactor, out string message)
    // {
    //   message = "None";
    //   double areaMissing;
    //   if(CheckOverallArea(out areaMissing) == false){
    //     //if(!inRevit)
    //     areaMissing *= 0.000001;
    //     message = string.Format("Floor plate not large enough, short by {0} sqm", areaMissing);
    //   }
    //   else
    //     message = "All areas fit.";

    //   {
    //     InitSubSpaces(_seamFactor, coreCrvs);
    //     _PlacedProgram = new Dictionary<int, List<SmSlivers>>();
    //     //smart slivers ordered by their origigal index
    //     var stSubs = _smSubSpaces.OrderBy(s => s._shiftIndex).ToList();

    //     for (int i = 0; i < _PlaceableSpaces.Count; i++)
    //     {
    //       string thing = "";
    //       TryPlace(_PlaceableSpaces[i], i, stSubs, out thing);
    //       outputString.Add(thing);
    //     }

    //     List<string> tempNames = new List<string>();
    //     for (int i = 0; i < _PlacedProgram.Keys.Count(); i++)
    //       tempNames.Add(_spaces[i]);

    //     ProcessPolygons(this._PlacedProgram, tempNames);
    //   }
    // }

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

      double threshold = 15.0;
      //if(!inRevit)
      threshold *= (_worldScale * _worldScale);

      while(Placed == false)
      {
        if(Math.Abs(areaAccumulated - space.area) < threshold){

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
                if(_PlacedProgram.TryGetValue(spaceIndex, out var listSpaces))
                {
                  listSpaces.Add(stSubs[twIndex]);
                }
                //if it doesnt...
                else
                {
                  _PlacedProgram.Add(spaceIndex, new List<SmSlivers>() {stSubs[twIndex]} );
                }
                 areaAccumulated += stSubs[twIndex]._poly.Area();
                _GlobalIndex++;
              }
            }
          }
          report = stSubs[_GlobalIndex]._shiftIndex.ToString();
          Placed = true;
          break;
        }

        if(_GlobalIndex >= this._SubSpaces.Count - 1)
          break;

        int twIndexy = stSubs[_GlobalIndex]._shiftIndex;
                // if dictionary index key exists
                if (_PlacedProgram.TryGetValue(spaceIndex, out var _listSpaces))
                {
                    _listSpaces.Add(stSubs[twIndexy]);
                }
                //if it doesnt...
                else
                {
                    _PlacedProgram.Add(spaceIndex, new List<SmSlivers>() { stSubs[twIndexy] });
                }
        areaAccumulated += stSubs[twIndexy]._poly.Area();

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
        //if(inRevit)
        ///  localArea = a * 0.000001;
        // else
        localArea = a;
        areaRequested += localArea;

      }

      if(_MainFace.Area() <= areaRequested){
        areaMissing = areaRequested - _MainFace.Area();
        suffArea = false;
      }
      return suffArea;
    }

    // public void InitSpaces()
    // {
    //   _PlaceableSpaces = new List<SmSpace>();

    //   for (int i = 0; i < _areas.Count; i++)
    //     _PlaceableSpaces.Add(new SmSpace(_spaces[i], _areas[i]));
    //     new SmSpace()
    // }


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
      var innerMostCorePolygon = _Core.Offset(-0.25, EndType.ClosedPolygon)[0];
      var initCoreLines = _Core.Segments();

       _Walls = new List<SmWall>();
       for ( int i = 0; i< innerMostCorePolygon.Segments().Length; i++)
       {
         _Walls.Add(new SmWall(i, innerMostCorePolygon.Segments()[i]));
       }
      // int ii = 0;
      // foreach(var i in _Core.Segments())
      // {
      //   _Walls.Add(new SmWall(ii, i.ExtendEnd(_leaseOffset)));
      //   ii++;
      // }
      // var perimLines = _boundaryPoly.Segments();

      // double extendAmount = 2.25 * 1.5;
      // extendAmount *= _worldScale;
       var _WallA = new SmWall[initCoreLines.Length];

      // Console.WriteLine("Perim lines: " + perimLines.Count().ToString());
      // Console.WriteLine("Core lines: " + initCoreLines.Length.ToString());
       int[] indices = Enumerable.Range(0, innerMostCorePolygon.Segments().Length).ToArray();
       System.Threading.Tasks.Parallel.ForEach(indices, (i) => {

      //   // original curve
      //   SmWall wallTemp;
        var startCorePt = initCoreLines[i].PointAt(0.0);
         var endCorePt = initCoreLines[i].PointAt(1.0);
          var v1 = initCoreLines[i].End - initCoreLines[i].Start;

          var crossDir = v1.Cross(Vector3.ZAxis).Unitized();
          var crossStart = startCorePt + crossDir * _leaseOffset;
           var crossEnd = endCorePt + crossDir * _leaseOffset;
      //   var vUnit = v1.Unitized();
      //   var elongWalls1 = new Line(initCoreLines[i].Start, vUnit, extendAmount + v1.Length());

      //   var otherLine = new Line(initCoreLines[i].End, vUnit, extendAmount);

      //   //outer curve
        
      //   var v2 = perimLines[i].End - perimLines[i].Start;
      //   var v2Unit = v2.Unitized();
      //   var elongWalls2 = new Line(perimLines[i].Start, v2Unit, extendAmount + v2.Length());

      //   var otherLine2 = new Line(perimLines[i].End, v2Unit, extendAmount);

      //   //ray from perimeter to curve
         Ray coreRay = new Ray(startCorePt, v1);
         //Ray perimRay = new Ray(otherLine.PointAt(0.0), v2);

        Vector3 intersectVec;
         if(IntersectsGroupOfLines(coreRay, innerMostCorePolygon.Segments(), out intersectVec))
         {
           if(Math.Abs((intersectVec- startCorePt).Length()- initCoreLines[i].Length()+ 0.25) < 1.0)
           {
           //use perim logic
           _WallA[i] = new SmWall(i, new Line(crossStart, crossEnd));
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
         });

       _Walls.AddRange(_WallA);
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

    // public Polygon [] SortGeo(List<Polygon> Polygons, Curve curve)
    // {
    //   SmAreaPt [] initAreas = new SmAreaPt[Polygons.Count];
    //   int[] indices = Enumerable.Range(0, Polygons.Count).ToArray();
    //   System.Threading.Tasks.Parallel.ForEach(indices, (i) => {
    //     var score = ComputeScoreByCurve(Polygons[i], curve);
    //     initAreas[i] = new SmAreaPt(Polygons[i], score);
    //     });

    //   var output = initAreas.OrderBy(i => i._score).Select(s => s._poly).ToArray();
    //   return output;
    // }

    // public double ComputeScoreByCurve(Polygon polygon, Curve curve)
    // {
    //   double closest_point_param;
    //   var localCentroid = polygon.Centroid();

    //   if (curve.ClosestPoint(localCentroid, out closest_point_param))
    //     return closest_point_param;
    //   else
    //     return -100.0;
    // }

    // public void InitSubSpaces(double _seamFactor, List<Curve> coreInputCrvs)
    // {
    //   var preQuads = _MainFace.Split(_Walls.Select(w => w._curve).ToList(), 0.001).ToList();

    //   _QuadAreas = SortGeo(preQuads);
    //   _SubSpaces = new List<Polygon>();
    //   indecesforPurgin = new List<int>();


    //   int pIntOffset = 0;
    //   tPoints = new List<Vector3>();

    //   for (int i = 0; i < _QuadAreas.Length; i++)
    //   {
    //     var PIndexes = new List<int>();
    //     var k = _Walls[i]._curve.Length();
    //     var faceIndex = i;

    //     var SCrvs = new List<Curve>();

    //     var dir = _Walls[i]._direction;

    //     dir /= dir.Length();

    //     var numMoves = (int) Math.Round(k / _SplitInterval);



    //     for (int j = 0; j < numMoves + 1; j++)
    //     {
    //       var stPt = _Walls[i]._curve.PointAt(0.0);
    //       Transform trans = Transform.Translation(dir * (j) *_SplitInterval);
    //       stPt.Transform(trans);

    //       Plane p = new Plane(stPt, dir);
    //       Curve [] crvOut;
    //       Point3d [] intPts;
    //       var intersect = Rhino.Geometry.Intersect.Intersection.PolygonPlane(_QuadAreas[faceIndex], p, 0.01, out crvOut, out intPts);
    //       if(crvOut != null)
    //         SCrvs.AddRange(crvOut);
    //     }

    //     var splitFaces = _QuadAreas[faceIndex].Split(SCrvs, 0.01);

    //     var sortedFaces = new List<Polygon>();

    //     sortedFaces = SortGeo(splitFaces.ToList(), _Walls[i]._curve).ToList();




    //     Plane testPlane = new Plane(_BoundaryCurve.PointAt(0.0), Vector3.ZAxis);


    //     for (int j = 0; j < sortedFaces.Count; j++)
    //     {

    //       for (int c = 0; c < coreInputCrvs.Count; c++)
    //       {
    //         var newC = coreInputCrvs[c].Transformed(new Transform(0,0, testPlane.Origin.Z));
    //         // var tform = Transform.PlanarProjection(testPlane);
    //         // newC.Transform(tform);

    //         var avgPt = sortedFaces[j].Vertices.Average();
    //         var centroid = sortedFaces[j].Centroid();
    //         tPoints.Add(centroid);
    //         if(newC.Contains(centroid, testPlane, 0.01) != PointContainment.Outside){
    //           sortedFaces.RemoveAt(j);
    //           j--;
    //         }
    //       }
    //     }

    //     sortedFaces = SortGeo(sortedFaces, _Walls[i]._curve).ToList();
    //     var sfCount = sortedFaces.Count;


    //     for (int s = 6; s >= 0; s--)
    //     {
    //       var subIndex = sfCount - 1 - s;
    //       var subModIndex = pIntOffset + subIndex;
    //       if(!PIndexes.Contains(subModIndex))
    //         PIndexes.Add(subModIndex);
    //     }


    //     _SubSpaces.AddRange(sortedFaces);
    //     indecesforPurgin.AddRange(PIndexes);
    //     pIntOffset += sfCount;
    //   }

    //   //Init smart slivers
    //   _smSubSpaces = new SmSlivers[_SubSpaces.Count];
    //   for (int g = 0; g < _smSubSpaces.Length; g++)
    //     _smSubSpaces[g] = new SmSlivers(g, _SubSpaces[g]);

    //   int modIndex = 0;
    //   int indexOffset = (int) (_seamFactor * (_SubSpaces.Count));
    //   var modSubSpaces = new Polygon[_SubSpaces.Count];

    //   var lastIndexContainer = new List<int>();

    //   for (int i = 0; i < _SubSpaces.Count; i++)
    //   {
    //     modIndex = (i + indexOffset) % _SubSpaces.Count;
    //     modSubSpaces[i] = _SubSpaces[modIndex];
    //     _smSubSpaces[i]._shiftIndex = modIndex;
    //   }

    //   _SubSpaces = modSubSpaces.ToList();
    // }

    public void ProcessPolygons(Dictionary<int, List<SmSlivers>> PolygonTree, List<string> spaceNames)
    {

      _ProcessedProgram = new Dictionary<int, SmSpace>();

      SmSpace [] initSpaces = new SmSpace[PolygonTree.Keys.Count()];
      int[] indices = Enumerable.Range(0, initSpaces.Length).ToArray();
      System.Threading.Tasks.Parallel.ForEach(indices, (index) => {
        if(PolygonTree.TryGetValue(index, out var branchSpaces))
        {
        var branchPolygons = branchSpaces.Select(s=>s._poly).ToList();;
        var mergedPolygon = Polygon.UnionAll(branchPolygons)[0];
        initSpaces[index] = new SmSpace(int.Parse(spaceNames[index]), mergedPolygon.Area());
        initSpaces[index].poly = mergedPolygon;

        _ProcessedProgram.Add(index, initSpaces[index]);
        }
        });

      PlacedSpaces = new List<SmSpace>();
      PlacedSpaces.AddRange(initSpaces.OrderBy(o => o.roomNumber).ToList());
    }
  }
}