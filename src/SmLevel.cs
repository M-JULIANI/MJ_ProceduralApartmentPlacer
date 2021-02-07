using Elements;
using Elements.Geometry;
using System.Collections.Generic;

namespace MJProceduralApartmentPlacer
{
public class SmLevel
  {
    public double _elevation;
    public int _index {get; set;}
    public List<SmFloorBoundary> _boundaries = new List<SmFloorBoundary>();

    public SmLevel(double elev)
    {
      _elevation = elev;
    }

    public SmLevel(double elev, List<SmFloorBoundary> boundaries)
    {
      _elevation = elev;
      _boundaries.AddRange(boundaries);
    }
    public SmLevel(double elev, SmFloorBoundary boundary)
    {
      _elevation = elev;
      _boundaries.Add(boundary);
    }
  }
}