using Godot;
using System;
using System.Collections.Generic;

public partial class MainLogic : Node3D
{
    private static readonly HeritageSite _testArea =
        HeritageLocations.Sites.Find(s => s.Name == "Bintana Point");

    private UIManager _uiManager;
    private LocationService _locationService;
    private RadarSystem _radarSystem;
    private HeritageDetector _heritageDetector;
    private ARCameraController _arCameraController;

    private Node3D _arCube;
    private Camera3D _activeCam;

    private double _displayLat;
    private double _displayLon;
    private double _targetLat;
    private double _targetLon;
    private bool _hasInitialPosition;
    private float _currentHeading;
    private bool _isGameStarted;

    public override void _Ready()
    {
        _arCube = GetNodeOrNull<Node3D>("ARCube");
        _activeCam = GetNodeOrNull<Camera3D>("Camera3D");

        _uiManager = new UIManager(this);
        _heritageDetector = new HeritageDetector(HeritageLocations.Sites);
        _radarSystem = new RadarSystem(_uiManager.RadarCenter, HeritageLocations.Sites);
        _locationService = new LocationService();
        _arCameraController = new ARCameraController();

        _uiManager.HideAll();
        _uiManager.HookUiEvents(OnStartPressed, OnLandmarkPressed, OnMapPressed, OnClosePlacesPressed, OnSpanishGatePressed);

        GetTree().CreateTimer(5.0).Timeout += () => _uiManager.ShowBootComplete();
        _locationService.LocationUpdated += OnLocationUpdate;
    }

    public override void _ExitTree()
    {
        _locationService.LocationUpdated -= OnLocationUpdate;
        _locationService.Stop();
    }

    private void OnStartPressed()
    {
        _isGameStarted = true;
        _uiManager.HideInfoPanel();
        _uiManager.HideStartButton();
        _uiManager.ShowMainHud();
        if (_arCube != null)
            _arCube.Hide();
        StartOldSystems();
    }

    private void OnLandmarkPressed()
    {
        _uiManager.TogglePlacesList();
        if (_arCube != null)
            _arCube.Hide();
    }

    private void OnMapPressed()
    {
        _uiManager.ToggleRadarVisibility();
    }

    private void OnClosePlacesPressed()
    {
        _uiManager.HidePlacesList();
    }

    private void OnSpanishGatePressed()
    {
        if (_arCube != null)
            _arCube.Show();

        _uiManager.ShowHeritageInfo(
            "Spanish Gate",
            "Spanish Gate is a historic colonial landmark that marks the entrance to the old city district. It has stood for centuries as a gateway to culture, trade, and community heritage."
        );
    }

    private void StartOldSystems()
    {
        _locationService.Start();
        StartCameraFeed();
    }

    private void StartCameraFeed()
    {
        if (CameraServer.Singleton == null)
            return;

        CameraServer.Singleton.Call("set_monitoring_feeds", true);
        GetTree().CreateTimer(1.5).Timeout += InitializeCameraFeed;
    }

    private void InitializeCameraFeed()
    {
        var feeds = CameraServer.Feeds();
        if (feeds.Count == 0)
            return;

        var feed = feeds[0];
        feed.Call("set_format", 0, new Godot.Collections.Dictionary());
        feed.Call("set_active", true);

        var camTex = new CameraTexture
        {
            CameraFeedId = feed.GetId(),
            WhichFeed = CameraServer.FeedImage.RgbaImage
        };

        _uiManager.ApplyCameraTexture(camTex, GetViewport().GetVisibleRect().Size);
    }

    public override void _Process(double delta)
    {
        if (!_isGameStarted)
            return;

        UpdateOrientation(delta);
        UpdateLocationState(delta);
    }

    private void UpdateOrientation(double delta)
    {
        var gyro = Input.GetGyroscope();
        _currentHeading = _arCameraController.IntegrateHeading(_currentHeading, delta, gyro.Y);
        _arCameraController.ApplyRotation(_activeCam, gyro, delta);
    }

    private void UpdateLocationState(double delta)
    {
        if (!_locationService.HasLocation)
        {
            _uiManager.UpdateWaitingNoLocation();
            return;
        }

        if (!_hasInitialPosition)
        {
            _displayLat = _locationService.Latitude;
            _displayLon = _locationService.Longitude;
            _hasInitialPosition = true;
        }

        _targetLat = _locationService.Latitude;
        _targetLon = _locationService.Longitude;

        // Full double precision lerp; clamp factor to [0,1] so frame spikes don't overshoot.
        double lerpFactor = Math.Min(10.0 * delta, 1.0);
        _displayLat += (_targetLat - _displayLat) * lerpFactor;
        _displayLon += (_targetLon - _displayLon) * lerpFactor;

        RefreshLocationDisplay();
    }

    private void RefreshLocationDisplay()
    {
        _uiManager.UpdateCoordinates(_displayLat, _displayLon);
        _radarSystem.Update(_displayLat, _displayLon, _currentHeading);

        double testDistance = GeoUtils.CalculateDistance(
            _displayLat, _displayLon,
            _testArea.Latitude, _testArea.Longitude);
        _uiManager.UpdateDistance(testDistance);

        CheckHeritageDistance(_displayLat, _displayLon);
    }

    private void OnLocationUpdate(double latitude, double longitude)
    {
        _targetLat = latitude;
        _targetLon = longitude;
    }

    private void CheckHeritageDistance(double latitude, double longitude)
    {
        var site = _heritageDetector.FindNearby(latitude, longitude);
        if (site.HasValue)
        {
            _uiManager.ShowHeritageInfo(site.Value.Name, site.Value.Description);
            return;
        }

        _uiManager.HideInfoPanel();
    }
}
