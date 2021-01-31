using Elements;
using Elements.Geometry;
using System.Collections.Generic;
   
namespace MJProceduralApartmentPlacer
{
  public class SmWall{
    public int _index;
    public Curve _curve;
    public Vector3 _direction;
    public Vector3 _normalDir;

    public bool _flipped {get; set;}

    public SmWall(int index, Curve curve)
    {
      _index = index;
      _curve = curve;
      var d = _curve.PointAt(0.0);
      _direction = new Vector3(_curve.PointAt(1.0) - _curve.PointAt(0.0));
      _normalDir = _direction.Cross(Vector3.ZAxis);
      _flipped = false;
    }
  }
}