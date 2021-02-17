using Elements;
using Elements.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MJProceduralApartmentPlacer
{
    public class SmSpace: IComparable
    {
        public int type { get; set; } //0, 1, 2
        public double area { get; set; }

        public double designArea { get; set; }
        public int roomNumber { get; set; } //room 1, 2, 3
        public double roomHeight { get; set; }
        public double sorter { get; set; }
        public SmLevel roomLevel {get; set;}
        public bool placed { get; set; }

        public Polygon poly { get; set; }

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
            this.area = Math.Abs(this.poly.Area());
        }
        public SmSpace(int type, int roomNumber, bool placed, double designArea)
        {
            this.type = type;
            this.roomNumber = roomNumber;
            this.sorter = roomNumber;
            this.placed = placed;
            this.designArea = designArea;
        }

        public SmSpace(int roomNumber, double designArea)
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
            for (int i = 0; i < array.Length; i++)
            {
                newSpaces[i] = new SmSpace(initList[i].type, array[i], false, initList[i].designArea);
                newSpaces[i].sorter = array[i];
            }

            var outSpaces = newSpaces.OrderBy(s => s.sorter).ToArray();

            return outSpaces;
        }

        public int CompareTo(object obj)
        {
            if(obj==null) return 1;

            SmSpace otherSpace= obj as SmSpace;
            if (otherSpace != null)
            return this.type.CompareTo(otherSpace.type);
                else
            throw new ArgumentException("Object is not a SmSpace");
        }

        public class SpaceTypeComparer : IEqualityComparer<SmSpace>
        {
            public bool Equals(SmSpace b1, SmSpace b2)
            {
                if (b2 == null && b1 == null)
                    return true;
                else if (b1 == null || b2 == null)
                    return false;
                else if (b1.type == b2.type)
                    return true;
                else
                    return false;
            }

            public int GetHashCode(SmSpace bx)
            {
                int hCode = bx.type;
                return hCode.GetHashCode();
            }
        }
    }


}