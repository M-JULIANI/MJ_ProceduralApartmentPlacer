using Elements;
using Elements.Geometry;
using System.Collections.Generic;
using System.Linq;

 namespace MJProceduralApartmentPlacer
{
public class SmFloorBoundary
  {
    public Polygon mainPoly;
    public Polygon offsetPoly;
    private double _worldScale = 1.0;

    public SmFloorBoundary(Polygon main)
    {
      mainPoly = main;
      InitOffset(mainPoly);
    }

    public void InitOffset(Polygon main)
    {
      Polygon [] offsets = new Polygon[2];
      offsets[0] = main.Offset(-0.5 * _worldScale, EndType.ClosedPolygon)[0];
      offsets[1] = main.Offset(0.5 * _worldScale, EndType.ClosedPolygon)[0];

      offsetPoly = offsets.OrderByDescending(o => o.Length()).ToList()[0];
    }
  }
}