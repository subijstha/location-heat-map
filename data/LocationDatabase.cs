// LocationDatabase.cs
// Handles all SQLite database operations for location persistence.

using SQLite;
using LocationHeatMap.Models;

namespace LocationHeatMap.Data
{
 
    /// Provides async SQLite data access methods for LocationPoint records.
    /// Uses the sqlite-net-pcl library for cross-platform compatibility.
    public class LocationDatabase
    {
        // Lazy-initialized async SQLite connection
        private SQLiteAsyncConnection? _database;

        // Radius in degrees within which two points are considered "same location"
        // ~111 meters per 0.001 degree
        private const double ClusterRadius = 0.0005;

    
        /// Initializes the database connection and ensures the table exists.
        /// Called once before the first DB operation.
        private async Task InitAsync()
        {
            if (_database is not null)
                return;

            // Store the database file in the app's local data directory
            var dbPath = Path.Combine(
                FileSystem.AppDataDirectory,
                "locations.db3"
            );

            _database = new SQLiteAsyncConnection(
                dbPath,
                SQLiteOpenFlags.ReadWrite |
                SQLiteOpenFlags.Create |
                SQLiteOpenFlags.SharedCache
            );

            // Create the LocationPoint table if it doesn't already exist
            await _database.CreateTableAsync<LocationPoint>();
        }

        
        /// Retrieves all stored location points from the database.
        /// <returns>List of all LocationPoint records.</returns>
        public async Task<List<LocationPoint>> GetAllLocationsAsync()
        {
            await InitAsync();
            return await _database!.Table<LocationPoint>().ToListAsync();
        }

    
        /// Saves a new location point to the database.
        /// If a nearby point already exists within the cluster radius,
        /// increments its visit count instead of inserting a duplicate.
        /// <param name="point">The location point to save.</param>
        public async Task SaveLocationAsync(LocationPoint point)
        {
            await InitAsync();

            // Fetch all existing points to check for nearby duplicates
            var allPoints = await _database!.Table<LocationPoint>().ToListAsync();

            // Find a point within the cluster radius
            var nearby = allPoints.FirstOrDefault(p =>
                Math.Abs(p.Latitude - point.Latitude) < ClusterRadius &&
                Math.Abs(p.Longitude - point.Longitude) < ClusterRadius
            );

            if (nearby != null)
            {
                // Increment visit count to increase heat intensity
                nearby.VisitCount++;
                nearby.Timestamp = point.Timestamp; // Update to latest time
                await _database.UpdateAsync(nearby);
            }
            else
            {
                // Insert as a new unique location point
                await _database.InsertAsync(point);
            }
        }

   
        /// Deletes all location records — used for data reset.
        public async Task ClearAllLocationsAsync()
        {
            await InitAsync();
            await _database!.DeleteAllAsync<LocationPoint>();
        }

       
        /// Returns the total count of stored location points.
        public async Task<int> GetLocationCountAsync()
        {
            await InitAsync();
            return await _database!.Table<LocationPoint>().CountAsync();
        }
    }
}
