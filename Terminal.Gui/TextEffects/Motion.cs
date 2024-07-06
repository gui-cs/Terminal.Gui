namespace Terminal.Gui.TextEffects;

public class Coord
{
    public int Column { get; set; }
    public int Row { get; set; }

    public Coord (int column, int row)
    {
        Column = column;
        Row = row;
    }

    public override string ToString () => $"({Column}, {Row})";
}

public class Waypoint
{
    public string WaypointId { get; set; }
    public Coord Coord { get; set; }
    public List<Coord> BezierControl { get; set; }

    public Waypoint (string waypointId, Coord coord, List<Coord> bezierControl = null)
    {
        WaypointId = waypointId;
        Coord = coord;
        BezierControl = bezierControl ?? new List<Coord> ();
    }
}

public class Segment
{
    public Waypoint Start { get; private set; }
    public Waypoint End { get; private set; }
    public double Distance { get; private set; }
    public bool EnterEventTriggered { get; set; }
    public bool ExitEventTriggered { get; set; }

    public Segment (Waypoint start, Waypoint end)
    {
        Start = start;
        End = end;
        Distance = CalculateDistance (start, end);
    }

    private double CalculateDistance (Waypoint start, Waypoint end)
    {
        // Add bezier control point distance calculation if needed
        return Math.Sqrt (Math.Pow (end.Coord.Column - start.Coord.Column, 2) + Math.Pow (end.Coord.Row - start.Coord.Row, 2));
    }

    public Coord GetCoordOnSegment (double distanceFactor)
    {
        int column = (int)(Start.Coord.Column + (End.Coord.Column - Start.Coord.Column) * distanceFactor);
        int row = (int)(Start.Coord.Row + (End.Coord.Row - Start.Coord.Row) * distanceFactor);
        return new Coord (column, row);
    }
}
public class Path
{
    public string PathId { get; private set; }
    public double Speed { get; set; }
    public Func<double, double> EaseFunction { get; set; }
    public int Layer { get; set; }
    public int HoldTime { get; set; }
    public bool Loop { get; set; }
    public List<Segment> Segments { get; private set; } = new List<Segment> ();
    public int CurrentStep { get; set; }
    public double TotalDistance { get; set; }
    public double LastDistanceReached { get; set; }
    public int MaxSteps => (int)Math.Ceiling (TotalDistance / Speed); // Calculates max steps based on total distance and speed

    public Path (string pathId, double speed, Func<double, double> easeFunction = null, int layer = 0, int holdTime = 0, bool loop = false)
    {
        PathId = pathId;
        Speed = speed;
        EaseFunction = easeFunction;
        Layer = layer;
        HoldTime = holdTime;
        Loop = loop;
    }

    public void AddWaypoint (Waypoint waypoint)
    {
        if (Segments.Count > 0)
        {
            var lastSegment = Segments.Last ();
            var newSegment = new Segment (lastSegment.End, waypoint);
            Segments.Add (newSegment);
            TotalDistance += newSegment.Distance;
        }
        else
        {
            var originWaypoint = new Waypoint ("origin", new Coord (0, 0));  // Assuming the path starts at origin
            var initialSegment = new Segment (originWaypoint, waypoint);
            Segments.Add (initialSegment);
            TotalDistance = initialSegment.Distance;
        }
    }

    public Coord Step ()
    {
        if (CurrentStep <= MaxSteps)
        {
            double progress = EaseFunction?.Invoke ((double)CurrentStep / TotalDistance) ?? (double)CurrentStep / TotalDistance;
            double distanceTravelled = TotalDistance * progress;
            LastDistanceReached = distanceTravelled;

            foreach (var segment in Segments)
            {
                if (distanceTravelled <= segment.Distance)
                {
                    double segmentProgress = distanceTravelled / segment.Distance;
                    return segment.GetCoordOnSegment (segmentProgress);
                }

                distanceTravelled -= segment.Distance;
            }
        }

        return Segments.Last ().End.Coord; // Return the end of the last segment if out of bounds
    }
}

public class Motion
{
    public Dictionary<string, Path> Paths { get; private set; } = new Dictionary<string, Path> ();
    public Path ActivePath { get; private set; }
    public Coord CurrentCoord { get; set; }
    public Coord PreviousCoord { get; set; }
    public EffectCharacter Character { get; private set; }  // Assuming EffectCharacter is similar to base_character.EffectCharacter

    public Motion (EffectCharacter character)
    {
        Character = character;
        CurrentCoord = new Coord (character.InputCoord.Column, character.InputCoord.Row);  // Assuming similar properties
        PreviousCoord = new Coord (-1, -1);
    }

    public void SetCoordinate (Coord coord)
    {
        CurrentCoord = coord;
    }

    public Path CreatePath (string pathId, double speed, Func<double, double> easeFunction = null, int layer = 0, int holdTime = 0, bool loop = false)
    {
        if (Paths.ContainsKey (pathId))
            throw new ArgumentException ($"A path with ID {pathId} already exists.");

        var path = new Path (pathId, speed, easeFunction, layer, holdTime, loop);
        Paths [pathId] = path;
        return path;
    }

    public Path QueryPath (string pathId)
    {
        if (!Paths.TryGetValue (pathId, out var path))
            throw new KeyNotFoundException ($"No path found with ID {pathId}.");

        return path;
    }

    public bool MovementIsComplete ()
    {
        return ActivePath == null || ActivePath.CurrentStep >= ActivePath.TotalDistance;
    }

    public void ActivatePath (Path path)
    {
        if (path == null)
            throw new ArgumentNullException (nameof (path), "Path cannot be null when activating.");

        ActivePath = path;
        ActivePath.CurrentStep = 0;  // Reset the path's progress
    }

    /// <summary>
    /// Set the active path to None if the active path is the given path.    
    /// </summary>
    public void DeactivatePath (Path p)
    {
        if (p == ActivePath)
        {
            ActivePath = null;
        }
    }
    public void DeactivatePath ()
    {
       ActivePath = null;        
    }

    public void Move ()
    {
        if (ActivePath != null)
        {
            PreviousCoord = CurrentCoord;
            CurrentCoord = ActivePath.Step ();
            ActivePath.CurrentStep++;

            if (ActivePath.CurrentStep >= ActivePath.TotalDistance)
            {
                if (ActivePath.Loop)
                    ActivePath.CurrentStep = 0;  // Reset the path for looping
                else
                    DeactivatePath ();  // Deactivate the path if it is not set to loop
            }
        }
    }

    public void ChainPaths (IEnumerable<Path> paths, bool loop = false)
    {
        var pathList = paths.ToList ();
        for (int i = 0; i < pathList.Count; i++)
        {
            var currentPath = pathList [i];
            var nextPath = i + 1 < pathList.Count ? pathList [i + 1] : pathList.FirstOrDefault ();

            // Here we could define an event system to trigger path activation when another completes
            // For example, you could listen for a "path complete" event and then activate the next path
            if (loop && nextPath != null)
            {
                // Implementation depends on your event system
            }
        }
    }
}
