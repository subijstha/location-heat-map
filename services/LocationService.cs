// LocationService.cs
// Manages GPS location tracking using .NET MAUI Geolocation API.

using LocationHeatMap.Data;
using LocationHeatMap.Models;
using Microsoft.Maui.Devices.Sensors;

namespace LocationHeatMap.Services
{
   
    /// Handles continuous GPS location tracking and persists each
    /// captured location to the SQLite database.
    
    public class LocationService
    {
        private readonly LocationDatabase _database;
        private CancellationTokenSource? _cancelTokenSource;
        private bool _isTracking = false;

        // Minimum distance (meters) before a new point is recorded
        private const double MinimumDistanceMeters = 10.0;

        // Last known location — used to filter redundant points
        private Location? _lastLocation;

        // Event raised when a new location is captured and saved
        public event EventHandler<LocationPoint>? LocationUpdated;

        public bool IsTracking => _isTracking;

        
        /// Initializes the service with a database instance.
        
        public LocationService(LocationDatabase database)
        {
            _database = database;
        }

        
        /// Starts continuous location tracking in the background.
        /// Requests permission before beginning.
        
        public async Task StartTrackingAsync()
        {
            if (_isTracking)
                return;

            // Request location permission at runtime
            var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                throw new PermissionException("Location permission was denied.");
            }

            _isTracking = true;
            _cancelTokenSource = new CancellationTokenSource();

            // Run the tracking loop on a background thread
            await Task.Run(() => TrackingLoopAsync(_cancelTokenSource.Token));
        }

        
        /// Stops the ongoing location tracking loop.
        
        public void StopTracking()
        {
            _cancelTokenSource?.Cancel();
            _isTracking = false;
            _lastLocation = null;
        }

        
        /// Core background loop — polls GPS every 5 seconds and
        /// saves significant position changes to the database.
        
        private async Task TrackingLoopAsync(CancellationToken token)
        {
            var request = new GeolocationRequest(
                GeolocationAccuracy.Best,
                TimeSpan.FromSeconds(10)
            );

            while (!token.IsCancellationRequested)
            {
                try
                {
                    // Get the current GPS location
                    var location = await Geolocation.GetLocationAsync(request, token);

                    if (location != null)
                    {
                        // Only save if moved beyond minimum distance threshold
                        if (ShouldSaveLocation(location))
                        {
                            var point = new LocationPoint
                            {
                                Latitude = location.Latitude,
                                Longitude = location.Longitude,
                                Accuracy = location.Accuracy ?? 0,
                                Timestamp = DateTime.UtcNow
                            };

                            await _database.SaveLocationAsync(point);
                            _lastLocation = location;

                            // Notify subscribers (e.g., ViewModel to refresh map)
                            LocationUpdated?.Invoke(this, point);
                        }
                    }

                    // Wait 5 seconds before the next poll
                    await Task.Delay(TimeSpan.FromSeconds(5), token);
                }
                catch (OperationCanceledException)
                {
                    // Graceful cancellation — exit the loop silently
                    break;
                }
                catch (Exception ex)
                {
                    // Log error but keep tracking alive
                    Console.WriteLine($"[LocationService] Error: {ex.Message}");
                    await Task.Delay(TimeSpan.FromSeconds(5), token);
                }
            }
        }

        
        /// Determines if the new location is far enough from the
        /// last saved location to be worth recording.
        
        private bool ShouldSaveLocation(Location newLocation)
        {
            if (_lastLocation == null)
                return true;

            var distanceKm = _lastLocation.CalculateDistance(
                newLocation.Latitude,
                newLocation.Longitude,
                DistanceUnits.Kilometers
            );

            // Convert km to meters and compare against threshold
            return distanceKm * 1000 >= MinimumDistanceMeters;
        }
    }
}
