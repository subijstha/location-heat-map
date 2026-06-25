// MainViewModel.cs
// ViewModel connecting the UI to location services and database.

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using LocationHeatMap.Data;
using LocationHeatMap.Models;
using LocationHeatMap.Services;
using Microsoft.Maui.Maps;
using Map = Microsoft.Maui.Controls.Maps.Map;

namespace LocationHeatMap.ViewModels
{
    
    /// ViewModel for the main map screen.
    /// Implements INotifyPropertyChanged for MVVM data binding.
    /// Manages location tracking state, map region, and heat map data.
    
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly LocationDatabase _database;
        private readonly LocationService _locationService;

        // ── Bindable Properties ────────────────────────────────────────────

        private bool _isTracking;
       
        public bool IsTracking
        {
            get => _isTracking;
            set { _isTracking = value; OnPropertyChanged(); OnPropertyChanged(nameof(TrackingButtonText)); }
        }

        private string _statusMessage = "Tap 'Start Tracking' to begin.";
        
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        private int _locationCount;
        /// <summary>Total number of saved location points.</summary>
        public int LocationCount
        {
            get => _locationCount;
            set { _locationCount = value; OnPropertyChanged(); }
        }

        public string TrackingButtonText =>
            IsTracking ? "⏹ Stop Tracking" : "▶ Start Tracking";

        // ── Map & Heat Map Data ────────────────────────────────────────────

        
        public ObservableCollection<LocationPoint> HeatMapPoints { get; } = new();

        private MapSpan _mapRegion = MapSpan.FromCenterAndRadius(
            new Location(37.3346, -122.0090), // Default: Apple Park, Cupertino
            Distance.FromKilometers(5)
        );

        public MapSpan MapRegion
        {
            get => _mapRegion;
            set { _mapRegion = value; OnPropertyChanged(); }
        }

        // ── Commands ───────────────────────────────────────────────────────

        public ICommand ToggleTrackingCommand { get; }

        public ICommand ClearDataCommand { get; }

        public ICommand RefreshMapCommand { get; }

        // ── Constructor ────────────────────────────────────────────────────

        public MainViewModel()
        {
            _database = new LocationDatabase();
            _locationService = new LocationService(_database);

            // Subscribe to location updates from the background service
            _locationService.LocationUpdated += OnLocationUpdated;

            // Wire up commands
            ToggleTrackingCommand = new Command(async () => await ToggleTrackingAsync());
            ClearDataCommand = new Command(async () => await ClearDataAsync());
            RefreshMapCommand = new Command(async () => await LoadHeatMapDataAsync());

            // Load existing data from the database on startup
            Task.Run(LoadHeatMapDataAsync);
        }

        // ── Command Handlers ───────────────────────────────────────────────

        
        /// Starts or stops GPS tracking based on current state.
        
        private async Task ToggleTrackingAsync()
        {
            if (IsTracking)
            {
                _locationService.StopTracking();
                IsTracking = false;
                StatusMessage = "Tracking stopped.";
            }
            else
            {
                try
                {
                    StatusMessage = "Requesting GPS permission...";
                    await _locationService.StartTrackingAsync();
                    IsTracking = true;
                    StatusMessage = "📍 Tracking active — move around to build your heat map!";
                }
                catch (PermissionException)
                {
                    StatusMessage = "❌ Location permission denied. Please enable in settings.";
                }
                catch (Exception ex)
                {
                    StatusMessage = $"❌ Error: {ex.Message}";
                }
            }
        }

        
        /// Clears all location data from the database and refreshes the UI.
        
        private async Task ClearDataAsync()
        {
            await _database.ClearAllLocationsAsync();
            HeatMapPoints.Clear();
            LocationCount = 0;
            StatusMessage = "🗑 All location data cleared.";
        }

        
        /// Loads all saved location points from the database
        /// and updates the heat map collection.
        
        public async Task LoadHeatMapDataAsync()
        {
            var points = await _database.GetAllLocationsAsync();

            // Update on the main thread to safely modify ObservableCollection
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                HeatMapPoints.Clear();
                foreach (var point in points)
                    HeatMapPoints.Add(point);

                LocationCount = points.Count;
                StatusMessage = $"Loaded {LocationCount} location points.";

                // Adjust map region to fit all recorded points
                if (points.Count > 0)
                    CenterMapOnPoints(points);
            });
        }

        
        /// Called when the background service records a new GPS point.
        /// Updates the heat map in real-time.
        
        private void OnLocationUpdated(object? sender, LocationPoint point)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                // Reload all points to reflect updated visit counts
                await LoadHeatMapDataAsync();
                StatusMessage = $"📍 New point saved: {point.Latitude:F4}, {point.Longitude:F4}";
            });
        }

        
        /// Calculates the bounding box of all points and centers the map.
        
        private void CenterMapOnPoints(List<LocationPoint> points)
        {
            double minLat = points.Min(p => p.Latitude);
            double maxLat = points.Max(p => p.Latitude);
            double minLng = points.Min(p => p.Longitude);
            double maxLng = points.Max(p => p.Longitude);

            double centerLat = (minLat + maxLat) / 2;
            double centerLng = (minLng + maxLng) / 2;

            // Add 20% padding around the bounding box
            double latSpan = (maxLat - minLat) * 1.2;
            double lngSpan = (maxLng - minLng) * 1.2;

            double radiusKm = Math.Max(latSpan, lngSpan) * 111; // ~111km per degree
            radiusKm = Math.Max(radiusKm, 0.5); // Minimum 500m radius

            MapRegion = MapSpan.FromCenterAndRadius(
                new Location(centerLat, centerLng),
                Distance.FromKilometers(radiusKm)
            );
        }

        // ── INotifyPropertyChanged ─────────────────────────────────────────

        public event PropertyChangedEventHandler? PropertyChanged;

        Raises PropertyChanged for the given property name.</summary>
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
