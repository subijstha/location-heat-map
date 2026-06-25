// LocationPoint.cs
// Model representing a single GPS location entry stored in SQLite.

using SQLite;

namespace LocationHeatMap.Models
{
   
    /// Represents a geographic location point captured from the device GPS.
    /// Each point is persisted to the local SQLite database.

    public class LocationPoint
    {
      
        /// Primary key — auto-incremented by SQLite.
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        /// Latitude of the captured location in decimal degrees.
    
        public double Latitude { get; set; }

      
        /// Longitude of the captured location in decimal degrees.
        public double Longitude { get; set; }

        
        /// Accuracy of the GPS reading in meters.
        
        public double Accuracy { get; set; }

        /// UTC timestamp when the location was recorded.
        public DateTime Timestamp { get; set; }

        /// Visit count — how many times this approximate location was recorded.
        /// Used to determine heat intensity on the map.
        public int VisitCount { get; set; } = 1;
    }
}
