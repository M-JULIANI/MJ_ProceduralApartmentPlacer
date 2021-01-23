using Elements;
using Elements.Geometry;
using System.Collections.Generic;
using System.Linq;

 namespace MJProceduralApartmentPlacer
{
public class SmFloorBoundary
  {
    public Curve mainCrv;
    public Curve offsetCrv;
    private double _worldScale = 1.0;

    public SmFloorBoundary(Curve main)
    {
      mainCrv = main;
      InitOffset();
    }

    public void InitOffset()
    {
      var pt = mainCrv.PointAt(0);
      var plane = new Plane(pt, Vector3.ZAxis);

      Polygon [] offsets = new Polygon[2];
      offsets[0] = mainCrv.ToPolyline().Offset(-1 * _worldScale, EndType.ClosedPolygon)[0];
      offsets[1] = mainCrv.ToPolyline().Offset(1 * _worldScale, EndType.ClosedPolygon)[0];

      offsetCrv = offsets.OrderByDescending(o => o.Length()).ToList()[0];
    }
  }
}