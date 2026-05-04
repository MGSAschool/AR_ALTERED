using Godot;
using Godot.Collections;
using System;

public class LocationService
{
    public event Action<double, double> LocationUpdated;

    public bool HasLocation { get; private set; }
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }

    private GodotObject _geoPlugin;
    private Callable _locationCallable;

    public void Start()
    {
        if (!Engine.HasSingleton("Geolocation"))
            return;

        _geoPlugin = Engine.GetSingleton("Geolocation");
        // Callable.From is required for plain C# objects; new Callable(obj, name)
        // only works when obj is a GodotObject.
        _locationCallable = Callable.From<Dictionary>(HandleLocationUpdate);
        _geoPlugin.Connect("location_update", _locationCallable);
        _geoPlugin.Call("start_updating_location");
    }

    public void Stop()
    {
        if (_geoPlugin == null)
            return;

        _geoPlugin.Call("stop_updating_location");
        _geoPlugin.Disconnect("location_update", _locationCallable);
        _geoPlugin = null;
        HasLocation = false;
    }

    private void HandleLocationUpdate(Dictionary location)
    {
        if (!location.ContainsKey("latitude") || !location.ContainsKey("longitude"))
            return;

        Latitude = Variant.From(location["latitude"]).AsDouble();
        Longitude = Variant.From(location["longitude"]).AsDouble();

        HasLocation = true;
        LocationUpdated?.Invoke(Latitude, Longitude);
    }
}
