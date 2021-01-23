using Elements;
using Elements.Geometry;
using System.Collections.Generic;
using System.Linq;

 namespace MJProceduralApartmentPlacer
{
 
 public class Engine
  {
    private List<SmSpace> _PlaceableSpaces;
    public List<SmWall> _Walls;


    private List<double> _areas;
    private List<string> _spaces;

    private int _GlobalIndex;

    public List<int> indecesforPurgin;


    Polyline _Core;
    public Curve _BoundaryCurve;
    Polyline _boundaryPoly;
    public Curve _SortingCurve;
    public Brep _MainFace;
    public Brep [] _QuadAreas;
    public List<Brep> _SubSpaces;

    public smSlivers [] _smSubSpaces;

    public DataTree<Brep> _PlacedProgram;
    public DataTree<Brep> _ProcessedProgram;

    double _SplitInterval;
    double _leaseOffset;
    double medOffset;

    public List<Curve> _Inters = new List<Curve>();
    public List<string> outputString = new List<string>();
    public Mesh inCoreM;
    public List<smLevel> inLvls;

    public List<SmSpace> PlacedSpaces;

    public List<string> _debugger;

    public List<Curve> coreCrvs;
    public List<Point3d> tPoints;

    public Engine(List<string> spaces, List<double> areas, double leaseDepth, List<smLevel> levels, List<Polyline> corePolys, double splitInterval)
    {
      _leaseOffset = leaseDepth * _worldScale;
      medOffset = _leaseOffset * 0.5;
      splitInterval *= _worldScale;

      inLvls = levels;

      coreCrvs = new List<Curve>();

      foreach(var p in corePolys)
      {
        var c = p.ToNurbsCurve();
        coreCrvs.Add(c);
      }

      var firstLevel = inLvls.OrderBy(l => l._elevation).ToList()[0];
      var boundary = firstLevel._boundaries[0].mainCrv;
      var orientation = boundary.ClosedCurveOrientation(Vector3d.ZAxis);

      if(orientation != CurveOrientation.Clockwise)
        boundary.Reverse();

      _areas = areas;
      _spaces = spaces;

      Polyline tempPoly;

      if(boundary.TryGetPolyline(out tempPoly))
        _boundaryPoly = tempPoly;
      _BoundaryCurve = boundary;



      _Core = InitCoreCrv();


      _SortingCurve = InitSortingCrv(_BoundaryCurve);

      if(_BoundaryCurve.SpanCount != _SortingCurve.SpanCount)
        _SortingCurve = _BoundaryCurve;
      //
      double tClosest;
      _SortingCurve.ClosestPoint(_BoundaryCurve.PointAt(0.0), out tClosest);
      _SortingCurve.ChangeClosedCurveSeam(tClosest);
      ///

      if(_SortingCurve.ClosedCurveOrientation(Vector3d.ZAxis) != _BoundaryCurve.ClosedCurveOrientation(Vector3d.ZAxis))
        _SortingCurve.Reverse();

      _SplitInterval = splitInterval;
      _GlobalIndex = 0;

      var crvs = new List<Curve>();
      crvs.Add(_BoundaryCurve);
      crvs.Add(_Core.ToNurbsCurve());
      _MainFace = Rhino.Geometry.Brep.CreatePlanarBreps(crvs, 0.01)[0];

      InitSpaces();
      InitWalls(out inCoreM);




    }

    public Curve InitSortingCrv(Curve boundaryCurve)
    {
      Curve selectedCrv;
      var offCrvs = new List<Curve>();
      var plane = new Plane(boundaryCurve.PointAt(0.0), Vector3d.ZAxis);
      var offset1 = boundaryCurve.Offset(plane, -medOffset, 0.1, CurveOffsetCornerStyle.Sharp)[0];
      var offset2 = boundaryCurve.Offset(plane, medOffset, 0.1, CurveOffsetCornerStyle.Sharp)[0];
      offCrvs.Add(offset1);
      offCrvs.Add(offset2);

      selectedCrv = offCrvs.OrderBy(o => o.GetLength()).ToList()[0];

      return selectedCrv;
    }

    public Polyline InitCoreCrv()
    {
      Polyline poly = new Polyline();
      Plane workPlane = new Plane(_boundaryPoly.PointAt(0.0), Vector3d.ZAxis);
      Curve insetOffset = _BoundaryCurve.Offset(workPlane, _leaseOffset, 0.1, CurveOffsetCornerStyle.Sharp)[0];

      Polyline polyOut;
      insetOffset.TryGetPolyline(out polyOut);

      if(polyOut.ToNurbsCurve().ClosedCurveOrientation(Vector3d.ZAxis) != _BoundaryCurve.ClosedCurveOrientation(Vector3d.ZAxis))
        polyOut.Reverse();

      return polyOut;
    }

    public void RunFirstFloor(double _seamFactor, out string message)
    {
      message = "Noe";
      double areaMissing;
      if(CheckOverallArea(out areaMissing) == false){
        //if(!inRevit)
        areaMissing *= 0.000001;
        message = String.Format("Floor plate not large enough, short by {0} sqm", areaMissing);
      }
      else
        message = "All areas fit.";

      {
        InitSubSpaces(_seamFactor, coreCrvs);
        _PlacedProgram = new DataTree<Brep>();

        //smart slivers ordered by their origigal index
        var stSubs = _smSubSpaces.OrderBy(s => s._shiftIndex).ToList();

        for (int i = 0; i < _PlaceableSpaces.Count; i++)
        {
          string thing = "";
          TryPlace(_PlaceableSpaces[i], i, stSubs, out thing);
          outputString.Add(thing);
        }

        List<string> tempNames = new List<string>();
        for (int i = 0; i < _PlacedProgram.BranchCount; i++)
          tempNames.Add(_spaces[i]);

        ProcessBreps(this._PlacedProgram, tempNames);
      }
    }

    public void TryStackBuilding(List<SmSpace> units, out List<string> outMess)
    {
      outMess = new List<string>();

      var sortedLvls = inLvls.OrderBy(l => l._elevation).ToList();
      for (int i = 1; i < sortedLvls.Count; i++)//exclude first level
      {
        var boundaries = sortedLvls[i]._boundaries;

        for (int j = 0; j < boundaries.Count; j++)
        {

          var mess = TryProject(units, boundaries[j].offsetCrv, boundaries[j].mainCrv, sortedLvls[i]);// ground floor units, various boundaries
          outMess.Add(mess.ToString());
        }

      }

    }

    public bool TryProject(List<SmSpace> firstLvlUnits, Curve offCrv, Curve mainCrv, smLevel level)
    {
      bool worked = false;
      var workPlane = new Plane(new Point3d(0, 0, level._elevation), Vector3d.ZAxis);

      for (int i = 0; i < firstLvlUnits.Count; i++)
      {
        var dupCrv = firstLvlUnits[i].curve.DuplicateCurve();
        var movedCrv = Rhino.Geometry.Curve.ProjectToPlane(dupCrv, workPlane); // projecting ground units to variable levels

        Polyline poly;
        if(movedCrv.TryGetPolyline(out poly))
        {
          //worked = true;
          var segments = poly.BreakAtAngles(20);

          var pts = segments.Select(s => s.PointAt(0)).ToList(); // getting unit crv poly pts
          string mess;
          bool inBool = AllPtsIn(offCrv, pts, out mess);

          if(inBool)
          {
            var unitN = new SmSpace(firstLvlUnits[i]._name, firstLvlUnits[i]._area);
            unitN.curve = movedCrv;
            PlacedSpaces.Add(unitN);
            worked = true;
          }
          else if (inBool == false && mess == "trim")
          {
            Curve crvOut;
            if(TrimKeep(mainCrv, movedCrv, out crvOut, level))
            {
              var unitN = new SmSpace(firstLvlUnits[i]._name, firstLvlUnits[i]._area);
              unitN.curve = crvOut;
              PlacedSpaces.Add(unitN);
              worked = true;
            }
          }
        }
        else
          continue;
      }

      return worked;

    }

    public bool TrimKeep(Curve trimCrv, Curve toTrim, out Curve crvOut, smLevel level)
    {
      bool trimmed = false;
      var crvs = new List<Curve>();
      var cutC = new List<Curve>();
      cutC.Add(trimCrv);
      crvs.Add(toTrim);
      var face = Rhino.Geometry.Brep.CreatePlanarBreps(crvs, 0.01)[0];
      crvOut = toTrim;

      var faces = face.Split(cutC, 0.1).ToList();

      Plane workPlane = new Plane(new Point3d(0, 0, level._elevation), Vector3d.ZAxis);

      for (int i = 0; i < faces.Count; i++)
      {
        if(trimCrv.Contains(faces[i].GetBoundingBox(true).Center, workPlane, 0.1) == PointContainment.Inside)
        {
          var nakedCrvs = faces[i].DuplicateNakedEdgeCurves(true, false);
          crvOut = Curve.JoinCurves(nakedCrvs)[0];
          return true;
        }

      }

      return trimmed;
    }


    public bool AllPtsIn(Curve curve, List<Point3d> pts, out string message)
    {
      bool allIn = true;
      message = "clean";

      int countIn = 0;


      Plane workPlane = new Plane(curve.PointAt(0), Vector3d.ZAxis);
      for (int i = 0; i < pts.Count; i++)
        if(curve.Contains(pts[i], workPlane, 0.1) == PointContainment.Inside)
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


    public void TryPlace(SmSpace space, int spaceIndex, List<smSlivers> stSubs, out string report)
    {
      bool Placed = false;
      var areaAccumulated = 0.0;
      report = "Placeholder!";

      double threshold = 15.0;
      //if(!inRevit)
      threshold *= (_worldScale * _worldScale);

      while(Placed == false)
      {
        if(Math.Abs(areaAccumulated - space._area) < threshold){

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

                _PlacedProgram.Add(stSubs[twIndex]._brep, new GH_Path(spaceIndex));
                areaAccumulated += stSubs[twIndex]._brep.GetArea();

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
        _PlacedProgram.Add(stSubs[twIndexy]._brep, new GH_Path(spaceIndex));
        areaAccumulated += stSubs[twIndexy]._brep.GetArea();

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

      if(_MainFace.GetArea() <= areaRequested){
        areaMissing = areaRequested - _MainFace.GetArea();
        suffArea = false;
      }
      return suffArea;
    }

    public void InitSpaces()
    {
      _PlaceableSpaces = new List<SmSpace>();

      for (int i = 0; i < _areas.Count; i++)
        _PlaceableSpaces.Add(new SmSpace(_spaces[i], _areas[i]));
    }

    public void InitWalls(out Mesh outMesh)
    {

      var insetCore = _Core.ToNurbsCurve();
      Plane plane = new Plane(_Core.PointAt(0.0), Vector3d.ZAxis);

      double offsetInset = 0.25;
      offsetInset *= _worldScale;

      var offsetCrvs = new List<Curve>();
      var insetOffset1 = insetCore.Offset(plane, -offsetInset, 0.01, CurveOffsetCornerStyle.Sharp)[0];
      var insetOffset2 = insetCore.Offset(plane, offsetInset, 0.01, CurveOffsetCornerStyle.Sharp)[0];
      offsetCrvs.Add(insetOffset1);
      offsetCrvs.Add(insetOffset2);
      var insetOffset = offsetCrvs.OrderBy(o => o.GetLength()).ToList()[0];


      double extrusionDepth = 5.0;
      extrusionDepth *= _worldScale;


      double moveDownLength = extrusionDepth * 0.5;

      var inCoreMesh = new Mesh();
      var extBrep = Extrusion.Create(insetOffset, extrusionDepth, false);

      Vector3d zDelta = new Vector3d(0, 0, plane.Origin.Z) - new Vector3d(0, 0, extBrep.GetBoundingBox(true).Center.Z);

      Transform moveDown = Transform.Translation(zDelta);
      extBrep.Transform(moveDown);
      var premeshBrep = extBrep.ToBrep();
      var mesher = Mesh.CreateFromBrep(premeshBrep, MeshingParameters.Default);
      inCoreMesh.Append(mesher);

      outMesh = inCoreMesh.DuplicateMesh();

      var boundaryMesh = new Mesh();
      var extBrep2 = Extrusion.Create(_boundaryPoly.ToNurbsCurve(), extrusionDepth, false);
      extBrep2.Transform(moveDown);
      var premeshBrep2 = extBrep2.ToBrep();
      var mesher2 = Mesh.CreateFromBrep(premeshBrep2, MeshingParameters.Default);
      boundaryMesh.Append(mesher2);

      _Walls = new List<SmWall>();

      var initCoreLines = _Core.BreakAtAngles(20);
      var perimLines = _boundaryPoly.BreakAtAngles(20);

      double extendAmount = 7.6 * 2.0;
      extendAmount *= _worldScale;
      var _WallA = new SmWall[initCoreLines.Length];
      int[] indices = Enumerable.Range(0, initCoreLines.Length).ToArray();
      System.Threading.Tasks.Parallel.ForEach(indices, (i) => {

        // original curve
        SmWall wallTemp;

        var elongWall = initCoreLines[i].ToNurbsCurve().Extend(0, extendAmount);
        var vec = new Vector3d(initCoreLines[i].Last - initCoreLines[i].First);
        vec.Unitize();
        var otherLine = new Line(initCoreLines[i].Last, vec * extendAmount);

        //outer curve
        var elongWall_O = perimLines[i].ToNurbsCurve().Extend(0, extendAmount);
        var vec_O = new Vector3d(perimLines[i].Last - perimLines[i].First);
        vec_O.Unitize();
        var otherLine_O = new Line(perimLines[i].Last, vec_O * extendAmount);

        //ray from perimeter to curve
        Ray3d coreRay = new Ray3d(otherLine_O.PointAt(0.0), initCoreLines[i].Last - initCoreLines[i].First);

        Ray3d perimRay = new Ray3d(otherLine.PointAt(0.0), perimLines[i].Last - perimLines[i].First);

        var intersect1 = Rhino.Geometry.Intersect.Intersection.MeshRay(inCoreMesh, coreRay);
        var intersect2 = Rhino.Geometry.Intersect.Intersection.MeshRay(boundaryMesh, perimRay);

        //if(intersect1 > 0.0 && Math.Abs(new Line(perimLines[i].First, coreRay.PointAt(intersect1)).Length - perimLines[i].Length) <= 7.6 * 2 * _worldScale)
        if(intersect1 > 0.0 && Math.Abs(new Line(initCoreLines[i].First, coreRay.PointAt(intersect1)).Length - initCoreLines[i].Length) <= 7.6 * 1 * _worldScale)
          // if(intersect1 > 0.0 )
        {
          var ln = new Line(perimLines[i].First, coreRay.PointAt(intersect1));
          var extendedCrv = ln.ToNurbsCurve();
          wallTemp = new SmWall(i, extendedCrv);
          wallTemp._curve = wallTemp._curve.Extend(CurveEnd.End, 1000.0, CurveExtensionStyle.Line);
          // _Walls.Add(new SmWall(i, extendedCrv));
        }
        else
        {
          //Run standard wall curve
          {
            var ln = new Line(initCoreLines[i].First, perimRay.PointAt(intersect2));

            var extendedCrv = ln.ToNurbsCurve();
            wallTemp = new SmWall(i, extendedCrv);
            wallTemp._curve = wallTemp._curve.Extend(CurveEnd.End, 1000.0, CurveExtensionStyle.Line);

          }
        }
        _WallA[i] = wallTemp;
        });

      _Walls.AddRange(_WallA);
    }

    public Brep [] SortGeo(List<Brep> breps)
    {
      smAreaPt [] initAreas = new smAreaPt[breps.Count];
      int[] indices = Enumerable.Range(0, breps.Count).ToArray();
      System.Threading.Tasks.Parallel.ForEach(indices, (i) => {
        var score = ComputeScoreByCurve(breps[i], _SortingCurve);
        initAreas[i] = new smAreaPt(breps[i], score);
        });

      var output = initAreas.OrderBy(i => i._score).Select(s => s._brep).ToArray();
      return output;
    }

    public Brep [] SortGeo(List<Brep> breps, Curve curve)
    {
      smAreaPt [] initAreas = new smAreaPt[breps.Count];
      int[] indices = Enumerable.Range(0, breps.Count).ToArray();
      System.Threading.Tasks.Parallel.ForEach(indices, (i) => {
        var score = ComputeScoreByCurve(breps[i], curve);
        initAreas[i] = new smAreaPt(breps[i], score);
        });

      var output = initAreas.OrderBy(i => i._score).Select(s => s._brep).ToArray();
      return output;
    }

    public double ComputeScoreByCurve(Brep brep, Curve curve)
    {
      double closest_point_param;
      var localCentroid = Rhino.Geometry.AreaMassProperties.Compute(brep).Centroid;

      if (curve.ClosestPoint(localCentroid, out closest_point_param))
        return closest_point_param;
      else
        return -100.0;
    }

    public void InitSubSpaces(double _seamFactor, List<Curve> coreInputCrvs)
    {
      var preQuads = _MainFace.Split(_Walls.Select(w => w._curve).ToList(), 0.001).ToList();

      _QuadAreas = SortGeo(preQuads);
      _SubSpaces = new List<Brep>();
      indecesforPurgin = new List<int>();


      int pIntOffset = 0;
      tPoints = new List<Point3d>();

      for (int i = 0; i < _QuadAreas.Length; i++)
      {
        var PIndexes = new List<int>();
        var k = _Walls[i]._curve.GetLength();
        var faceIndex = i;

        var SCrvs = new List<Curve>();

        var dir = _Walls[i]._direction;

        dir /= dir.Length;

        var numMoves = (int) Math.Round(k / _SplitInterval);



        for (int j = 0; j < numMoves + 1; j++)
        {
          var stPt = _Walls[i]._curve.PointAt(0.0);
          Transform trans = Transform.Translation(dir * (j) *_SplitInterval);
          stPt.Transform(trans);

          Plane p = new Plane(stPt, dir);
          Curve [] crvOut;
          Point3d [] intPts;
          var intersecty = Rhino.Geometry.Intersect.Intersection.BrepPlane(_QuadAreas[faceIndex], p, 0.01, out crvOut, out intPts);
          if(crvOut != null)
            SCrvs.AddRange(crvOut);
        }

        var splitFaces = _QuadAreas[faceIndex].Split(SCrvs, 0.01);

        var sortedFaces = new List<Brep>();

        sortedFaces = SortGeo(splitFaces.ToList(), _Walls[i]._curve).ToList();




        Plane testPlane = new Plane(_BoundaryCurve.PointAt(0.0), Vector3d.ZAxis);


        for (int j = 0; j < sortedFaces.Count; j++)
        {

          for (int c = 0; c < coreInputCrvs.Count; c++)
          {
            var newC = coreInputCrvs[c].DuplicateCurve();
            var tform = Transform.PlanarProjection(testPlane);
            newC.Transform(tform);

            var v = sortedFaces[j].DuplicateVertices();
            var avgPt = AveragePt(v);
            var amp = Rhino.Geometry.AreaMassProperties.Compute(sortedFaces[j]);
            var centroid = amp.Centroid;
            tPoints.Add(centroid);
            if(newC.Contains(centroid, testPlane, 0.01) != PointContainment.Outside){
              sortedFaces.RemoveAt(j);
              j--;
            }
          }
        }

        sortedFaces = SortGeo(sortedFaces, _Walls[i]._curve).ToList();
        var sfCount = sortedFaces.Count;


        for (int s = 6; s >= 0; s--)
        {
          var subIndex = sfCount - 1 - s;
          var subModIndex = pIntOffset + subIndex;
          if(!PIndexes.Contains(subModIndex))
            PIndexes.Add(subModIndex);
        }


        _SubSpaces.AddRange(sortedFaces);
        indecesforPurgin.AddRange(PIndexes);
        pIntOffset += sfCount;
      }

      //Init smart slivers
      _smSubSpaces = new smSlivers[_SubSpaces.Count];
      for (int g = 0; g < _smSubSpaces.Length; g++)
        _smSubSpaces[g] = new smSlivers(g, _SubSpaces[g]);

      int modIndex = 0;
      int indexOffset = (int) (_seamFactor * (_SubSpaces.Count));
      var modSubSpaces = new Brep[_SubSpaces.Count];

      var lastIndexContainer = new List<int>();

      for (int i = 0; i < _SubSpaces.Count; i++)
      {
        modIndex = (i + indexOffset) % _SubSpaces.Count;
        modSubSpaces[i] = _SubSpaces[modIndex];
        _smSubSpaces[i]._shiftIndex = modIndex;
      }

      _SubSpaces = modSubSpaces.ToList();
    }

    public void ProcessBreps(DataTree<Brep> brepTree, List<string> spaceNames)
    {

      _ProcessedProgram = new DataTree<Brep>();

      SmSpace [] initSpaces = new SmSpace[brepTree.BranchCount];
      int[] indices = Enumerable.Range(0, brepTree.BranchCount).ToArray();
      System.Threading.Tasks.Parallel.ForEach(indices, (index) => {
        var branch = brepTree.Branch(index);
        var mergedBrep = Brep.MergeBreps(branch, 0.01);
        var nakedCrvs = mergedBrep.DuplicateNakedEdgeCurves(true, false);
        var jC = Curve.JoinCurves(nakedCrvs)[0];

        var recreated = Brep.CreatePlanarBreps(jC, 0.01)[0];

        initSpaces[index] = new SmSpace(spaceNames[index], recreated.GetArea());
        initSpaces[index].curve = jC;
        initSpaces[index].index = index;

        _ProcessedProgram.Add(recreated, new GH_Path(index));
        });

      PlacedSpaces = new List<SmSpace>();
      PlacedSpaces.AddRange(initSpaces.OrderBy(o => o.index).ToList());
    }
  }
}