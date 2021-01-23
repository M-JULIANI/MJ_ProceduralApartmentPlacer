  namespace MJProceduralApartmentPlacer
{
  public class SmSpace
      {
        public int type {get; set;} //0, 1, 2
        public double area {get; set;}
        public int roomNumber {get; set;} //room 1, 2, 3

        public bool placed {get; set;}

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
      }
}