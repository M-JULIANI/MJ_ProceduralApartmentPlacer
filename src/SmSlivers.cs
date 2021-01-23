using Elements;
using Elements.Geometry;
using System.Collections.Generic;

   namespace MJProceduralApartmentPlacer
{
  public class SmSlivers
  {
    public int _stIndex;
    public int _shiftIndex {get;set;}
    public Polygon _poly;

    public SmSlivers(int stIndex, Polygon poly)
    {
      _stIndex = stIndex;
      _poly = poly;
    }
  }
}