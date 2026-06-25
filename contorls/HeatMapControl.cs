// HeatMapControl.cs
// Custom GraphicsView that renders the heat map overlay on top of the map.

using Microsoft.Maui.Graphics;
using LocationHeatMap.Models;

namespace LocationHeatMap.Controls
{
    
    /// A custom drawable that renders heat map circles for each
    /// LocationPoint on a transparent canvas overlay.
    /// Intensity is indicated by color: blue (low) → red (high).
    
    public class HeatMapDrawable : IDrawable
    {
        // Collection of location points to render
        public List<LocationPoint> Points { get; set; } = new();

        // Bounds of the visible map area (in lat/lng)
        public double MinLat { get; set; }
        public double MaxLat { get; set; }
        public double MinLng { get; set; }
        public double MaxLng { get; set; }

        // Maximum visit count — used for relative intensity normalization
        public int MaxVisitCount { get; set; } = 1;

        // Radius of each heat circle in pixels
        private const float HeatRadius = 40f;

        
        /// Draws all heat map circles onto the canvas.
        /// Color shifts from blue → green → yellow → red based on intensity.
        
        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            if (Points == null || Points.Count == 0)
                return;

            foreach (var point in Points)
            {
                // Convert geographic coordinates to pixel coordinates
                var (x, y) = LatLngToPixel(
                    point.Latitude,
                    point.Longitude,
                    dirtyRect
                );

                // Normalize visit count to 0.0–1.0 for color interpolation
                float intensity = Math.Min(
                    (float)point.VisitCount / MaxVisitCount,
                    1.0f
                );

                // Interpolate color: blue(0) → cyan → green → yellow → red(1)
                var heatColor = InterpolateHeatColor(intensity);

                // Draw outer glow (semi-transparent, larger radius)
                canvas.FillColor = heatColor.WithAlpha(0.15f);
                canvas.FillCircle(x, y, HeatRadius * 1.8f);

                // Draw mid glow
                canvas.FillColor = heatColor.WithAlpha(0.3f);
                canvas.FillCircle(x, y, HeatRadius * 1.2f);

                // Draw solid core
                canvas.FillColor = heatColor.WithAlpha(0.7f);
                canvas.FillCircle(x, y, HeatRadius * 0.6f);
            }
        }

        
        /// Converts latitude/longitude to pixel coordinates within the canvas bounds.
        /// Uses linear interpolation across the visible map extent.
        
        private (float x, float y) LatLngToPixel(
            double lat, double lng, RectF bounds)
        {
            // Guard against zero-range bounds
            var latRange = MaxLat - MinLat;
            var lngRange = MaxLng - MinLng;

            if (latRange == 0 || lngRange == 0)
                return (bounds.Width / 2f, bounds.Height / 2f);

            float x = (float)((lng - MinLng) / lngRange * bounds.Width);
            // Latitude is inverted: higher lat = lower y pixel
            float y = (float)((1 - (lat - MinLat) / latRange) * bounds.Height);

            return (x, y);
        }

        
        /// Produces a heat map color from a 0–1 intensity value.
        /// 0 = Blue (cold/low), 0.5 = Green, 0.75 = Yellow, 1 = Red (hot/high).
        
        private Color InterpolateHeatColor(float t)
        {
            // Color stops: Blue → Cyan → Green → Yellow → Red
            if (t < 0.25f)
            {
                float s = t / 0.25f;
                return new Color(0f, s, 1f); // Blue → Cyan
            }
            else if (t < 0.5f)
            {
                float s = (t - 0.25f) / 0.25f;
                return new Color(0f, 1f, 1f - s); // Cyan → Green
            }
            else if (t < 0.75f)
            {
                float s = (t - 0.5f) / 0.25f;
                return new Color(s, 1f, 0f); // Green → Yellow
            }
            else
            {
                float s = (t - 0.75f) / 0.25f;
                return new Color(1f, 1f - s, 0f); // Yellow → Red
            }
        }
    }
}
