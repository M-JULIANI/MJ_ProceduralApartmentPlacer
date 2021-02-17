using Elements;
using Elements.Geometry;
using Elements.Geometry.Solids;
using System;
using System.Collections.Generic;
using System.Linq;
//using GeometryEx;
using System.Threading.Tasks;

namespace MJProceduralApartmentPlacer
{

    public class PlacementEngine
    {
        //private List<SmSpace> _PlaceableSpaces;
        public SmWall[] _Walls;


        private List<double> _areas;
        private List<string> _spaces;

        public List<SmWall> coreLinesViz;

        private int _GlobalIndex;

        public List<int> indecesforPurgin;


        Polygon _Core;
        public Polyline _BoundaryCurve;
        Polyline _boundaryPoly;
        public IList<Polygon> _MainFace;
        public Polygon[] _SubSpaces;
        public List<Polygon> semiSlivers;
        public List<SmSlivers>[] _Slivers;

        public SmSlivers[] _smSubSpaces;

        //previously datatree<polygon>
        public Dictionary<int, List<SmSlivers>> _PlacedProgramSlivers;
        public List<SmSpace> _PlacedProgramSpaces;
        public Dictionary<int, SmSpace> _ProcessedProgram;

        double _SplitInterval;
        double _leaseOffset;
        double medOffset;

        private List<SmLevel> inLvls;
        private SmLevel firstLevel;

        public List<Vector3> startPts;
        public List<SmSpace> PlacedSpaces;
        public List<SmSpace> FirstFloorSpaces;
        public List<SmSpace> _PlaceableSpaces;
        List<SmSpace> distinctSpaces;


        public List<Polygon> coreCrvs;
        public Polygon boundary;

        private double _worldScale = 1.0;

        public PlacementEngine()
        {

        }
        public PlacementEngine(List<SmSpace> spaces, double leaseDepth, List<SmLevel> levels, double splitInterval, IList<Polygon> corePolys = null)
        {
            _leaseOffset = leaseDepth * _worldScale;
            medOffset = _leaseOffset * 0.5;
            splitInterval *= _worldScale;

            inLvls = levels;

            coreCrvs = new List<Polygon>();

            if (corePolys != null)
            {
                foreach (var p in corePolys)
                {
                    var c = p;
                    coreCrvs.Add(c);
                }
            }

            firstLevel = inLvls.OrderBy(l => l._elevation).ToList()[0];
            firstLevel._index = 0;
            boundary = firstLevel._boundaries[0].mainPoly;

            //    System.IO.File.WriteAllText( "D:/Hypar/offsetTest.json", Newtonsoft.Json.JsonConvert.SerializeObject(boundary));

            // var orientation = boundary.ClosedCurveOrientation(Vector3d.ZAxis);
            var ssspaces = SmSpace.Jitter(spaces, 0.99).ToList();

            distinctSpaces = ssspaces.GroupBy(x => x.type).Select(y => y.First()).ToList();

            _areas = ssspaces.OrderBy(s => s.sorter).Select(s => s.designArea).ToList();
            _spaces = ssspaces.OrderBy(s => s.sorter).Select(s => s.roomNumber.ToString()).ToList();

            _PlaceableSpaces = ssspaces;
            Polyline tempPoly = boundary.ToPolyline();

            _boundaryPoly = tempPoly;
            _BoundaryCurve = boundary.ToPolyline();

            Console.WriteLine(boundary.ToString());

            _Core = InitCoreCrv(boundary);

            _SplitInterval = splitInterval;
            _GlobalIndex = 0;

            _MainFace = boundary.Difference(_Core);

            InitWalls();

            _PlacedProgramSpaces = new List<SmSpace>();
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
                    // Console.WriteLine(i.ToString() + " " + thing);
                }

                List<string> tempNames = new List<string>();
                for (int i = 0; i < _PlacedProgramSlivers.Keys.Count(); i++)
                    tempNames.Add(_spaces[i]);

                ProcessPolygons(_PlacedProgramSlivers, tempNames);
            }
        }

        /// <summary>
        /// Stacks rooms on building levels.
        /// </summary>
        /// <param name="units"></param>
        /// <param name="outMess"></param>
        public void TryStackBuilding(out List<string> outMess)
        {
            outMess = new List<string>();
            PlacedSpaces = new List<SmSpace>();

            var sortedLvls = inLvls.OrderBy(l => l._elevation).ToList();

            for (int i = 0; i < sortedLvls.Count; i++)//exclude first level
            {
                var lvlHeightToNext = 2.0;

                if (i != sortedLvls.Count - 1)
                {
                    lvlHeightToNext = sortedLvls[i + 1]._elevation - sortedLvls[i]._elevation;

                    var boundaries = sortedLvls[i + 1]._boundaries;
                    sortedLvls[i]._index = i;
                    sortedLvls[i]._levelHeightToNext = lvlHeightToNext;

                    for (int j = 0; j < boundaries.Count; j++)
                    {

                        var transformedOffset = boundaries[j].offsetPoly.TransformedPolygon(new Transform(0, 0, sortedLvls[i]._elevation));
                        var transformedMain = boundaries[j].mainPoly.TransformedPolygon(new Transform(0, 0, sortedLvls[i]._elevation));

                        var mess = TryProject(FirstFloorSpaces, transformedOffset, transformedMain, sortedLvls[i]);// ground floor units, various boundaries
                        outMess.Add(mess.ToString());
                    }
                }
            }
        }

        /// <summary>
        ///  Adds 'placed spaces' to PlacedSpaces list.
        /// </summary>
        /// <param name="firstLvlUnits"></param>
        /// <param name="offCrv"></param>
        /// <param name="mainCrv"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public bool TryProject(List<SmSpace> firstLvlUnits, Polygon offCrv, Polygon mainCrv, SmLevel level)
        {
            bool worked = false;

            for (int i = 0; i < firstLvlUnits.Count; i++)
            {
                var dupCrv = new Polygon(firstLvlUnits[i].poly.Vertices);

                var movedCrv = dupCrv.TransformedPolygon(new Transform(new Vector3(0, 0, offCrv.Centroid().Z)));

                var pts = movedCrv.Vertices.ToList(); // getting unit crv poly pts
                string mess;
                bool inBool = AllPtsIn(offCrv, pts, out mess);

                int s = -1;
                var newRmNum = firstLvlUnits[i].roomNumber.ToString().Remove(0, 1).Insert(0, level._index.ToString());

                int parsedRmNum;
                if (Int32.TryParse(newRmNum, out parsedRmNum))
                    s = parsedRmNum;

                if (inBool)
                {
                    var unitN = new SmSpace(firstLvlUnits[i].type, s, true, firstLvlUnits[i].designArea, movedCrv);
                    unitN.roomLevel = level;
                    PlacedSpaces.Add(unitN);
                    worked = true;
                }
                else if (inBool == false && mess == "trim")
                {
                    Polygon crvOut;
                    if (TrimKeep(mainCrv, movedCrv, level, out crvOut))
                    {
                        var designArea = firstLvlUnits[i].designArea;


                        if (Math.Abs(crvOut.Area() / designArea) >= 0.75)
                        {
                            var unitN = new SmSpace(firstLvlUnits[i].type, s, true, firstLvlUnits[i].designArea, crvOut);
                            unitN.roomLevel = level;
                            PlacedSpaces.Add(unitN);
                            worked = true;
                        }
                        else //try finding a closest best unit fit
                        {
                            var closestDistinctUnit = FindClosestUnitType(crvOut, distinctSpaces);

                            if (Math.Abs(crvOut.Area() / closestDistinctUnit.designArea) >= 0.75)
                            {
                                var unitN = new SmSpace(closestDistinctUnit.type, s, true, closestDistinctUnit.designArea, crvOut);
                                unitN.roomLevel = level;
                                PlacedSpaces.Add(unitN);
                                worked = true;
                            }
                        }

                    }
                }
            }
            return worked;
        }

        Polygon ReturnInsidePoly(List<Polygon> candidates, Polygon arbiter)
        {
            if (candidates == null || arbiter == null)
                return null;
            Polygon empty = null;
            for (int i = 0; i < candidates.Count; i++)
            {
                if (arbiter.Contains(candidates[i].Centroid()))
                    return candidates[i];
            }
            return empty;
        }

        public SmSpace FindClosestUnitType(Polygon poly, List<SmSpace> distinctSpaces)
        {
            if (poly != null && distinctSpaces != null)
                return distinctSpaces.OrderBy(s => Math.Abs(s.designArea - poly.Area())).First();

            return null;
        }

        public bool TrimKeep(Polygon trimCrv, Polygon toTrim, SmLevel level, out Polygon crvOut)
        {
            bool trimmed = false;
            crvOut = null;

            var diffResults = toTrim.Intersection(trimCrv);

            if (diffResults == null)
                return false;

            var findInsidePoly = ReturnInsidePoly(diffResults.ToList(), trimCrv);

            if (findInsidePoly != null)
            {
                trimmed = true;
                crvOut = findInsidePoly;
            }

            return trimmed;
        }


        public bool AllPtsIn(Polygon curve, List<Vector3> pts, out string message)
        {
            bool allIn = true;
            message = "clean";

            int countIn = 0;

            for (int i = 0; i < pts.Count; i++)
                if (curve.Contains(pts[i], out var contaimment))
                    if (contaimment == Containment.Inside)
                        countIn++;

            if (countIn == 0)
                allIn = false;
            else if (countIn == pts.Count)
                allIn = true;
            else
                allIn = false;

            if (countIn != 0 && countIn != pts.Count)
                message = "trim";

            return allIn;
        }


        public void TryPlace(SmSpace space, int spaceIndex, List<SmSlivers> stSubs, out string report)
        {
            //Console.WriteLine("TOTAL SLIVER COUNT: "+ semiSlivers.Count);
            //Console.WriteLine("TOTAL stubs: "+ stSubs.Count);

            bool Placed = false;
            var areaAccumulated = 0.0;
            report = "Placeholder!";

            double threshold = _leaseOffset * _SplitInterval;
            //if(!inRevit)
            threshold *= (_worldScale * _worldScale);

            while (Placed == false)
            {
                if (_GlobalIndex >= semiSlivers.Count - 1)
                    break;
                //Console.WriteLine("current GLOBAL: " + _GlobalIndex);
                //Console.WriteLine($"index: {spaceIndex}, areaAccum: {areaAccumulated}");
                if (Math.Abs(areaAccumulated - space.designArea) <= threshold)
                {
                    if (_GlobalIndex >= semiSlivers.Count - 1)
                        break;
                    if (indecesforPurgin.Contains(stSubs[_GlobalIndex]._stIndex))
                    {
                        bool placedExtras = false;

                        while (placedExtras == false)
                        {

                            if (_GlobalIndex >= semiSlivers.Count - 1)
                                break;
                            if (indecesforPurgin.Contains(stSubs[_GlobalIndex]._stIndex) == false)
                            {
                                placedExtras = true;
                                Placed = true;

                            }
                            else
                            {

                                int twIndex = stSubs[_GlobalIndex]._shiftIndex;

                                // if dictionary index key exists
                                if (_PlacedProgramSlivers.TryGetValue(spaceIndex, out var listSpaces))
                                {
                                    listSpaces.Add(stSubs[twIndex]);
                                    // var newList = new List<Polygon>();
                                    // newList.AddRange(listSpaces);
                                    // _PlacedProgramSlivers[spaceIndex] = newList;

                                }
                                //if it doesnt...
                                else
                                {
                                    _PlacedProgramSlivers.Add(spaceIndex, new List<SmSlivers>() { stSubs[twIndex] });
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

                if (_GlobalIndex >= this.semiSlivers.Count - 1)
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
                    _PlacedProgramSlivers.Add(spaceIndex, new List<SmSlivers>() { stSubs[twIndexy] });
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
            foreach (var a in _areas)
            {
                localArea = a;
                areaRequested += localArea;

            }

            if (Math.Abs(_MainFace[0].Area()) - Math.Abs(_MainFace[1].Area()) <= areaRequested)
            {
                areaMissing = areaRequested - Math.Abs(_MainFace[0].Area());
                suffArea = false;
            }
            return suffArea;
        }

        bool IntersectsGroupOfLines(Ray ray, Line[] linesToIntersect, out Vector3 firstVec)
        {
            bool hit = false;
            firstVec = Vector3.Origin;

            for (int i = 0; i < linesToIntersect.Length; i++)
            {
                Vector3 hitResult;
                if (ray.Intersects(linesToIntersect[i], out hitResult))
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
            for (int i = 0; i < innerMostCorePolygon.Segments().Length; i++)
            {
                coreLinesViz.Add(new SmWall(i, innerMostCorePolygon.Segments()[i]));
            }

            var _WallA = new SmWall[initCoreLines.Length];
            startPts = new List<Vector3>();

            for (int i = 0; i < innerMostCorePolygon.Segments().Length; i++)
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
                if (IntersectsGroupOfLines(coreRay, innerMostCorePolygon.Segments(), out intersectVec))
                {
                    var intersectDist = (intersectVec - startCorePt).Length();
                    var origDist = initCoreLines[i].Length();
                    if (Math.Abs(origDist - intersectDist) + internalestOffsetDist < _leaseOffset)
                    {
                        //use perim logic
                        _WallA[i] = new SmWall(i, new Line(crossStart, crossEnd));
                        _WallA[i]._flipped = true;
                    }
                    else
                    {
                        // _WallA[i] = new SmWall(i, initCoreLines[i].ExtendEnd(_leaseOffset)
                        _WallA[i] = new SmWall(i, Utils.ExtendLineByEnd(initCoreLines[i], _leaseOffset));
                    }
                }
                else
                {
                    // _WallA[i] = new SmWall(i, initCoreLines[i].ExtendEnd(_leaseOffset));
                    _WallA[i] = new SmWall(i, Utils.ExtendLineByEnd(initCoreLines[i], _leaseOffset));

                }
                // });

            }

            _Walls = _WallA;

        }

        public Polygon[] SortGeo(List<SmSlivers> Slivers, Curve curve, bool flipped)
        {
            SmAreaPt[] initAreas = new SmAreaPt[Slivers.Count];
            //int[] indices = Enumerable.Range(0, Polygons.Count).ToArray();
            // System.Threading.Tasks.Parallel.ForEach(indices, (i) => 
            for (int i = 0; i < Slivers.Count; i++)
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
            if (flipped)
                measurePt = curve.PointAt(0.0);
            var dist = measurePt.DistanceTo(localCentroid);
            return dist;
        }

        bool ContainedInCoreCrvs(SmSlivers sliver, List<Polygon> corePolys)
        {
            bool contained = false;

            if (corePolys != null && corePolys.Count > 0)
            {
                for (int c = 0; c < corePolys.Count; c++)
                {
                    if (corePolys[c].Contains(sliver._poly.Centroid()))
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
                var closestSeg = sortingPoly.Segments().OrderBy(s => s.PointAt(0.5).DistanceTo(authorityWall._curve.PointAt(0.5))).ToList()[0];

                Vector3[] segPts = new Vector3[2];
                segPts[0] = closestSeg.PointAt(0.0);
                segPts[1] = closestSeg.PointAt(1.0);

                var sortedPts = segPts.OrderBy(s => s.DistanceTo(authorityWall._curve.PointAt(0.0))).ToArray();

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

            double crvLength = 0.0;
            List<Line> splitLines = new List<Line>();
            Line ln;


            //creation of new zones
            for (int w = 0; w < _Walls.Length; w++)
            {
                crvLength = _Walls[w]._curve.Length();
                var numSlivers = (int)Math.Floor(crvLength / _SplitInterval);


                ln = new Line(_Walls[w]._curve.PointAt(0.0), _Walls[w]._curve.PointAt(1.0));
                splitLines = ln.DivideByLength(numSlivers, false);

                var crossDir = Vector3.ZAxis;

                if (_Walls[w]._flipped == true)
                    crossDir *= -1.0;

                var diagPoint = ln.Start + _Walls[w]._direction.Cross(crossDir).Unitized() * _leaseOffset + (ln.End - ln.Start);
                var p = Polygon.Rectangle(ln.Start, diagPoint);
                _SubSpaces[w] = p;

            }


            var otherSubSpaces = new Polygon[_Walls.Length];
            for (int i = 0; i < _SubSpaces.Length; i++)
            {
                Polygon resultingPoly = null;

                Polygon[] offsets = new Polygon[1];

                if (i == 0)
                {
                    offsets[0] = _SubSpaces[_SubSpaces.Length - 1];
                    var splitters = _SubSpaces[i].Difference(offsets);

                    if (splitters != null)
                    {
                        if (splitters.Count == 2)
                        {
                            resultingPoly = splitters[1];
                        }
                        else
                            resultingPoly = splitters[0];

                        otherSubSpaces[i] = resultingPoly;
                    }
                }
                else
                {

                    offsets[0] = _SubSpaces[i - 1];
                    var splitters = _SubSpaces[i].Difference(offsets);
                    if (splitters != null)
                    {
                        if (splitters.Count == 2)
                        {
                            resultingPoly = splitters[1];
                        }
                        else
                            resultingPoly = splitters[0];

                        otherSubSpaces[i] = resultingPoly;
                    }
                }
            }
            _SubSpaces = otherSubSpaces;

            var tempWalls = new SmWall[_Walls.Length];

            for (int w = 0; w < _Walls.Length; w++)
            {

                var newWallSeg = AlignWallPlease(_SubSpaces[w], _Walls[w]);


                tempWalls[w] = new SmWall(w, newWallSeg);

                if (_Walls[w]._flipped)
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
                splitLines = ln.DivideByLength(_SplitInterval, false);

                var tempList = new List<SmSlivers>();

                for (int i = 0; i < splitLines.Count; i++)
                {
                    var crossDir = Vector3.ZAxis;

                    if (_Walls[w]._flipped == true)
                        crossDir *= -1.0;

                    var diagPoint = splitLines[i].Start + _Walls[w]._direction.Cross(crossDir).Unitized() * _leaseOffset + (splitLines[i].End - splitLines[i].Start);
                    var p = Polygon.Rectangle(splitLines[i].Start, diagPoint);

                    if (_SubSpaces[counter].Contains(p.Centroid()))
                        tempList.Add(new SmSlivers(i, p));

                }
                _Slivers[counter] = tempList;
                counter++;
            }


            for (int j = 0; j < _Slivers.Length; j++)
            {
                var PIndexes = new List<int>();

                var sortedFaces = _Slivers[j].ToList();

                for (int s = 0; s < sortedFaces.Count; s++)
                {
                    if (ContainedInCoreCrvs(sortedFaces[s], coreCrvs))
                    {
                        sortedFaces.RemoveAt(s);
                        s--;
                    }
                }

                var sfCount = sortedFaces.Count;

                for (int s = 6; s >= 0; s--)
                {
                    var subIndex = sfCount - s;
                    var subModIndex = pIntOffset + subIndex;
                    if (!PIndexes.Contains(subModIndex))
                        PIndexes.Add(subModIndex);
                }


                semiSlivers.AddRange(sortedFaces.OrderBy(s => s._stIndex).Select(s => s._poly));
                indecesforPurgin.AddRange(PIndexes);
                pIntOffset += sfCount;
            }


            //Init smart slivers
            _smSubSpaces = new SmSlivers[semiSlivers.Count];
            for (int g = 0; g < _smSubSpaces.Length; g++)
                _smSubSpaces[g] = new SmSlivers(g, semiSlivers[g]);

            int modIndex = 0;
            int indexOffset = (int)(_seamFactor * (semiSlivers.Count));
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
            //SmSpace[] initSpaces = new SmSpace[PolygonTree.Keys.Count()];

            try
            {

                for (int i = 0; i < PolygonTree.Keys.Count; i++)
                {
                    if (PolygonTree.TryGetValue(i, out var branchSpaces))
                    {

                        if (branchSpaces != null && branchSpaces.Count > 0)
                        {
                            var branchPolygons = branchSpaces.Select(s => s._poly).ToList();
                            var rawUnion = Polygon.UnionAll(branchPolygons)[0];



                            if (rawUnion != null)
                            {

                                if (Math.Abs(rawUnion.Area()) / _PlaceableSpaces[i].designArea >= 0.25)
                                {
                                    var space = new SmSpace(_PlaceableSpaces[i].type, _PlaceableSpaces[i].roomNumber, true, _PlaceableSpaces[i].designArea, rawUnion);
                                    space.roomLevel = firstLevel;
                                    space.sorter = i;
                                    _ProcessedProgram.Add(i, space);
                                }
                            }
                        }

                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            FirstFloorSpaces = new List<SmSpace>();
            FirstFloorSpaces.AddRange(_ProcessedProgram.Values.ToList());
        }
    }
}