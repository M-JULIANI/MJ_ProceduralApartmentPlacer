using Elements;
using Elements.Geometry;
using System.Collections.Generic;
   
namespace MJProceduralApartmentPlacer
{
  public class SmAreaPt
  {
    public Polygon _poly;
    public Vector3 _point;
    public double _score;

    public SmAreaPt(Polygon poly, double score)
    {
      _poly = poly;
      _point = _poly.Centroid();
      _score = score;
    }
  }
}