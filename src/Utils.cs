using Elements;
using Elements.Geometry;
using System;
using System.Linq;

namespace MJProceduralApartmentPlacer
{
    public static class Utils
    {
        public static Vector3 AveragePt(Vector3[] pts)
        {
            double avgX = 0.0;
            double avgY = 0.0;
            double avgZ = 0.0;

            for (int i = 0; i < pts.Length; i++)
            {
                avgX += pts[i].X;
                avgY += pts[i].Y;
                avgZ += pts[i].Z;
            }

            avgX /= pts.Length;
            avgY /= pts.Length;
            avgZ /= pts.Length;

            return new Vector3(avgX, avgY, avgZ);
        }

        public static Line ExtendLineByEnd(Line line, double distance)
        {

            Vector3 dir = line.End - line.Start;
            var unitizedDir = dir.Unitized();

            Line outLine = new Line(line.Start, unitizedDir, distance + line.Length());
            return outLine;
        }
    }

    
}