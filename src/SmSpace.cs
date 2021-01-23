using Elements;
using Elements.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace MJProceduralApartmentPlacer
{
  public class SmSpace
      {
        public int type {get; set;} //0, 1, 2
        public double area {get; set;}
        public int roomNumber {get; set;} //room 1, 2, 3
        public double roomHeight {get; set;}

        public bool placed {get; set;}

        public Polygon poly {get; set;}

        public SmSpace()
        {
          type = -1;
          roomNumber = -1;
          placed = false;
          area = -1;
        }

        public SmSpace(int type, int roomNumber, bool placed, double area)
        {
          this.type = type;
          this.roomNumber = roomNumber;
          this.placed = placed;
          this.area = area;
        }

        public SmSpace(int roomNumber, double area)
        {
          this.roomNumber = roomNumber;
          this.area = area;
        }
      }
}