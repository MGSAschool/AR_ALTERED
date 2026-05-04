using Godot;
using System;
using System.Collections.Generic;

public class RadarSystem
{
    // 500 m maps to the radar half-width (150 px); sites beyond 500 m go off-screen.
    private const float WorldScale = 150.0f / 500.0f;
    private const double LatitudeToMeters = 111111.0;

    private readonly Control _center;
    private readonly List<HeritageSite> _sites;
    private readonly Dictionary<string, Sprite2D> _siteIcons = new();
    private readonly Texture2D _iconTexture;

    public RadarSystem(Control center, IEnumerable<HeritageSite> sites)
    {
        _center = center;
        _sites = new List<HeritageSite>(sites);
        _iconTexture = GD.Load<Texture2D>("res://icon.svg");
        SetupRadar();
    }

    private void SetupRadar()
    {
        if (_center == null || _iconTexture == null)
            return;

        foreach (var site in _sites)
        {
            var icon = new Sprite2D();
            icon.Texture = _iconTexture;
            icon.Scale = new Vector2(0.12f, 0.12f);
            _center.AddChild(icon);
            _siteIcons[site.Name] = icon;
        }
    }

    public void Update(double playerLat, double playerLon, float headingRadians)
    {
        if (_center == null || _siteIcons.Count == 0)
            return;

        double cosLat = Math.Cos(playerLat * Math.PI / 180.0);

        foreach (var site in _sites)
        {
            if (!_siteIcons.TryGetValue(site.Name, out var icon))
                continue;

            double dLat = (site.Latitude - playerLat) * LatitudeToMeters;
            double dLon = (site.Longitude - playerLon) * LatitudeToMeters * cosLat;
            var position = new Vector2((float)dLon, (float)-dLat) * WorldScale;
            icon.Position = position.Rotated(headingRadians);
        }
    }
}
