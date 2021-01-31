using Elements;
using Elements.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MJProceduralApartmentPlacer
{
  public class SmSpace
      {
        public int type {get; set;} //0, 1, 2
        public double area {get; set;}

         public double designArea {get; set;}
        public int roomNumber {get; set;} //room 1, 2, 3
        public double roomHeight {get; set;}
        public double sorter {get; set;}

        public bool placed {get; set;}

        public Polygon poly {get; set;}

        public SmSpace()
        {
          type = -1;
          roomNumber = -1;
          placed = false;
          area = -1;
        }



        public SmSpace(int type, int roomNumber, bool placed, double designArea, Polygon poly)
        {
          this.type = type;
          this.roomNumber = roomNumber;
          this.sorter = roomNumber;
          this.placed = placed;
          this.designArea = designArea;
          this.poly = poly;
          this.area = this.poly.Area();
        }
        public SmSpace(int type, int roomNumber, bool placed, double designArea)
        {
          this.type = type;
          this.roomNumber = roomNumber;
          this.sorter = roomNumber;
          this.placed = placed;
          this.designArea = designArea;
        }

        public SmSpace(int roomNumber, double area)
        {
          this.roomNumber = roomNumber;
          this.sorter = roomNumber;
          this.designArea = designArea;
        }

         public static SmSpace[] Jitter(List<SmSpace> initList, double jitterFactor)
        {
            int[] array = new int[initList.Count];

            double maxDistance = jitterFactor * array.Length;
            Random r = new Random(42);

            for (int i = 0; i < array.Length; i++)
            {
                double min = Math.Max(i - maxDistance, 0);
                double max = Math.Min(i + maxDistance, array.Length);

                var item = r.Next((int)min, (int)max);
                array[i] = item;
            }
            var newSpaces = new SmSpace[array.Length];
            for(int i=0; i< array.Length; i++)
            {
              newSpaces[i] = new SmSpace(initList[i].type, initList[i].roomNumber, false, initList[i].area);
              newSpaces[i].sorter = array[i];     
            }

            var outSpaces = newSpaces.OrderBy(s=>s.sorter).ToArray();

            return outSpaces;
        }
      }
}