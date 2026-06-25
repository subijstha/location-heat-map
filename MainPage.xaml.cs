// MainPage.xaml.cs
// Code-behind for MainPage — wires the heat map drawable to the
// GraphicsView and redraws it whenever location data changes.

using LocationHeatMap.Controls;
using LocationHeatMap.ViewModels;
using Microsoft.Maui.Maps;

namespace LocationHeatMap
{
    
    /// Code-behind for the main map screen.
    /// Connects the ViewModel's heat map data to the custom GraphicsView drawable.
    
    public partial class MainPage : ContentPage
    {
        // The heat map drawing logic
        private readonly HeatMapDrawable _heatMapDrawable;

        public MainPage()
        {
            InitializeComponent();

            // Initialize the heat map renderer
            _heatMapDrawable = new HeatMapDrawable();
            HeatMapCanvas.Drawable = _heatMapDrawable;
        }

        /// <inheritdoc/>
        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Subscribe to ViewModel property changes to refresh heat map
            if (BindingContext is MainViewModel vm)
            {
                vm.PropertyChanged += ViewModel_PropertyChanged;
                vm.HeatMapPoints.CollectionChanged += HeatMapPoints_CollectionChanged;
            }
        }

        /// <inheritdoc/>
        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            // Unsubscribe to avoid memory leaks
            if (BindingContext is MainViewModel vm)
            {
                vm.PropertyChanged -= ViewModel_PropertyChanged;
                vm.HeatMapPoints.CollectionChanged -= HeatMapPoints_CollectionChanged;
            }
        }

        
        /// Redraws the heat map whenever the HeatMapPoints collection changes.
        
        private void HeatMapPoints_CollectionChanged(
            object? sender,
            System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            RefreshHeatMap();
        }

        
        /// Redraws the heat map when the map region changes (pan/zoom).
      
        private void ViewModel_PropertyChanged(
            object? sender,
            System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.MapRegion))
            {
                // Update the map's visible region
                if (BindingContext is MainViewModel vm)
                    MainMap.MoveToRegion(vm.MapRegion);

                RefreshHeatMap();
            }
        }

        
        /// Updates the drawable's data and triggers a canvas redraw.
        /// Calculates the visible geographic bounds from the current map region.
       
        private void RefreshHeatMap()
        {
            if (BindingContext is not MainViewModel vm)
                return;

            var points = vm.HeatMapPoints.ToList();
            if (points.Count == 0)
            {
                HeatMapCanvas.Invalidate();
                return;
            }

            // Calculate bounding box for lat/lng → pixel conversion
            _heatMapDrawable.Points = points;
            _heatMapDrawable.MinLat = points.Min(p => p.Latitude);
            _heatMapDrawable.MaxLat = points.Max(p => p.Latitude);
            _heatMapDrawable.MinLng = points.Min(p => p.Longitude);
            _heatMapDrawable.MaxLng = points.Max(p => p.Longitude);
            _heatMapDrawable.MaxVisitCount = points.Max(p => p.VisitCount);

            // Trigger canvas redraw
            HeatMapCanvas.Invalidate();
        }
    }
}
