namespace Terminal.Gui.TextEffects;


public static class GeometryUtils
{

    public static List<Coord> FindCoordsOnCircle (Coord origin, int radius, int coordsLimit = 0, bool unique = true)
    {
        var points = new List<Coord> ();
        var seenPoints = new HashSet<Coord> ();
        if (coordsLimit == 0)
            coordsLimit = (int)Math.Ceiling (2 * Math.PI * radius);
        double angleStep = 2 * Math.PI / coordsLimit;

        for (int i = 0; i < coordsLimit; i++)
        {
            double angle = i * angleStep;
            int x = (int)(origin.Column + radius * Math.Cos (angle));
            int y = (int)(origin.Row + radius * Math.Sin (angle));
            var coord = new Coord (x, y);

            if (unique && !seenPoints.Contains (coord))
            {
                points.Add (coord);
                seenPoints.Add (coord);
            }
            else if (!unique)
            {
                points.Add (coord);
            }
        }

        return points;
    }

    public static List<Coord> FindCoordsInCircle (Coord center, int diameter)
    {
        var coordsInEllipse = new List<Coord> ();
        int radius = diameter / 2;
        for (int x = center.Column - radius; x <= center.Column + radius; x++)
        {
            for (int y = center.Row - radius; y <= center.Row + radius; y++)
            {
                if (Math.Pow (x - center.Column, 2) + Math.Pow (y - center.Row, 2) <= Math.Pow (radius, 2))
                    coordsInEllipse.Add (new Coord (x, y));
            }
        }
        return coordsInEllipse;
    }

    public static List<Coord> FindCoordsInRect (Coord origin, int distance)
    {
        var coords = new List<Coord> ();
        for (int column = origin.Column - distance; column <= origin.Column + distance; column++)
        {
            for (int row = origin.Row - distance; row <= origin.Row + distance; row++)
            {
                coords.Add (new Coord (column, row));
            }
        }
        return coords;
    }

    public static Coord FindCoordAtDistance (Coord origin, Coord target, double distance)
    {
        double totalDistance = FindLengthOfLine (origin, target) + distance;
        double t = distance / totalDistance;
        int nextColumn = (int)((1 - t) * origin.Column + t * target.Column);
        int nextRow = (int)((1 - t) * origin.Row + t * target.Row);
        return new Coord (nextColumn, nextRow);
    }

    public static Coord FindCoordOnBezierCurve (Coord start, List<Coord> controlPoints, Coord end, double t)
    {
        // Implementing De Casteljau's algorithm for Bezier curve
        if (controlPoints.Count == 1) // Quadratic
        {
            double x = Math.Pow (1 - t, 2) * start.Column +
                       2 * (1 - t) * t * controlPoints [0].Column +
                       Math.Pow (t, 2) * end.Column;
            double y = Math.Pow (1 - t, 2) * start.Row +
                       2 * (1 - t) * t * controlPoints [0].Row +
                       Math.Pow (t, 2) * end.Row;
            return new Coord ((int)x, (int)y);
        }
        else if (controlPoints.Count == 2) // Cubic
        {
            double x = Math.Pow (1 - t, 3) * start.Column +
                       3 * Math.Pow (1 - t, 2) * t * controlPoints [0].Column +
                       3 * (1 - t) * Math.Pow (t, 2) * controlPoints [1].Column +
                       Math.Pow (t, 3) * end.Column;
            double y = Math.Pow (1 - t, 3) * start.Row +
                       3 * Math.Pow (1 - t, 2) * t * controlPoints [0].Row +
                       3 * (1 - t) * Math.Pow (t, 2) * controlPoints [1].Row +
                       Math.Pow (t, 3) * end.Row;
            return new Coord ((int)x, (int)y);
        }
        throw new ArgumentException ("Invalid number of control points for bezier curve");
    }

    public static Coord FindCoordOnLine (Coord start, Coord end, double t)
    {
        int x = (int)((1 - t) * start.Column + t * end.Column);
        int y = (int)((1 - t) * start.Row + t * end.Row);
        return new Coord (x, y);
    }

    public static double FindLengthOfBezierCurve (Coord start, List<Coord> controlPoints, Coord end)
    {
        double length = 0.0;
        Coord prevCoord = start;
        for (int i = 1; i <= 10; i++)
        {
            double t = i / 10.0;
            Coord coord = FindCoordOnBezierCurve (start, controlPoints, end, t);
            length += FindLengthOfLine (prevCoord, coord);
            prevCoord = coord;
        }
        return length;
    }

    public static double FindLengthOfLine (Coord coord1, Coord coord2)
    {
        return Math.Sqrt (Math.Pow (coord2.Column - coord1.Column, 2) +
                         Math.Pow (coord2.Row - coord1.Row, 2));
    }

    public static double FindNormalizedDistanceFromCenter (int maxRow, int maxColumn, Coord otherCoord)
    {
        double center_x = maxColumn / 2.0;
        double center_y = maxRow / 2.0;
        double maxDistance = Math.Sqrt (Math.Pow (maxColumn, 2) + Math.Pow (maxRow, 2));
        double distance = Math.Sqrt (Math.Pow (otherCoord.Column - center_x, 2) +
                                    Math.Pow (otherCoord.Row - center_y, 2));
        return distance / (maxDistance / 2);
    }
}