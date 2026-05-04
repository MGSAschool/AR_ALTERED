using Godot;
using System;

public class UIManager
{
    private const string UiRoot = "CanvasLayer/MarginContainer/UI_Root/";

    private readonly Panel      _infoPanel;
    private readonly Label      _infoPanelTitle;
    private readonly RichTextLabel _infoPanelDescription;
    private readonly Button     _infoPanelCloseBtn;
    private readonly Label      _coordLabel;
    private readonly Label      _distanceLabel;
    private readonly TextureRect _cameraDisplay;
    private readonly Control    _radarContainer;
    private readonly ColorRect  _bootSplash;
    private readonly Button     _startButton;
    private readonly ColorRect  _placesListUI;
    private readonly Button     _closePlacesBtn;
    private readonly Button     _landmarkBtn;
    private readonly Button     _mapBtn;
    private readonly Button     _spanishGateBtn;

    private enum GpsState { Unknown, Waiting, Live }
    private GpsState _gpsState    = GpsState.Unknown;
    private double   _lastLat     = double.NaN;
    private double   _lastLon     = double.NaN;
    private double   _lastDist    = double.NaN;
    private string   _shownSiteTitle;

    public Control RadarCenter { get; }

    public UIManager(Node root)
    {
        _coordLabel           = root.GetNodeOrNull<Label>(UiRoot + "CoordLabel");
        _distanceLabel        = root.GetNodeOrNull<Label>("CanvasLayer/BintanaLabel");
        _infoPanel            = root.GetNodeOrNull<Panel>(UiRoot + "InfoPanel");
        _infoPanelTitle       = _infoPanel?.GetNodeOrNull<Label>("Label");
        _infoPanelDescription = _infoPanel?.GetNodeOrNull<RichTextLabel>("RichTextLabel");
        _infoPanelCloseBtn    = _infoPanel?.GetNodeOrNull<Button>("Button");
        _cameraDisplay        = root.GetNodeOrNull<TextureRect>("CanvasLayer/CameraDisplay");
        _radarContainer       = root.GetNodeOrNull<Control>(UiRoot + "Minimap");
        RadarCenter           = _radarContainer?.GetNodeOrNull<Control>("Center");
        _bootSplash           = root.GetNodeOrNull<ColorRect>("CanvasLayer/BootSplash");
        _startButton          = root.GetNodeOrNull<Button>("CanvasLayer/StartButton");
        _placesListUI         = root.GetNodeOrNull<ColorRect>("CanvasLayer/PlacesList");
        _closePlacesBtn       = root.GetNodeOrNull<Button>("CanvasLayer/PlacesList/Button");
        _landmarkBtn          = root.GetNodeOrNull<Button>("CanvasLayer/LandmarkButton");
        _mapBtn               = root.GetNodeOrNull<Button>("CanvasLayer/MapButton");
        _spanishGateBtn       = root.GetNodeOrNull<Button>("CanvasLayer/PlacesList/LandmarkList/VBox/SpanishGateButton");

        if (_infoPanelCloseBtn != null)
            _infoPanelCloseBtn.Pressed += HideInfoPanel;
    }

    // ── Boot sequence ─────────────────────────────────────────────────────────

    public void HideAll()
    {
        _infoPanel?.Hide();
        _coordLabel?.Hide();
        _distanceLabel?.Hide();
        _cameraDisplay?.Hide();
        _radarContainer?.Hide();
        _placesListUI?.Hide();
        _startButton?.Hide();
        _landmarkBtn?.Hide();
        _mapBtn?.Hide();
        _bootSplash?.Show();
    }

    public void ShowBootComplete()
    {
        if (_bootSplash != null)
        {
            var tween = _bootSplash.CreateTween();
            tween.TweenProperty(_bootSplash, "modulate:a", 0.0f, 0.6f);
            tween.TweenCallback(Callable.From(() => _bootSplash.Hide()));
        }

        if (_startButton != null)
        {
            _startButton.Text     = "BEGIN EXPLORING";
            _startButton.Modulate = new Color(1, 1, 1, 0);
            _startButton.Show();
            var tween = _startButton.CreateTween();
            tween.TweenProperty(_startButton, "modulate:a", 1.0f, 0.4f);
        }
    }

    public void ShowMainHud()
    {
        _coordLabel?.Show();
        _distanceLabel?.Show();
        _cameraDisplay?.Show();
        _landmarkBtn?.Show();
        _mapBtn?.Show();
    }

    // ── GPS / location ────────────────────────────────────────────────────────

    public void UpdateWaitingNoLocation()
    {
        if (_gpsState == GpsState.Waiting) return;
        _gpsState = GpsState.Waiting;
        _lastLat  = double.NaN;
        _lastLon  = double.NaN;
        if (_coordLabel != null)
            _coordLabel.Text = "GPS  Acquiring signal...";
    }

    public void UpdateCoordinates(double latitude, double longitude)
    {
        bool stateChanged = _gpsState != GpsState.Live;
        bool posChanged   = Math.Abs(_lastLat - latitude) > 1e-7 ||
                            Math.Abs(_lastLon - longitude) > 1e-7;
        if (!stateChanged && !posChanged) return;

        _gpsState = GpsState.Live;
        _lastLat  = latitude;
        _lastLon  = longitude;

        if (_coordLabel != null)
            _coordLabel.Text = $"GPS  {latitude:F5}°  {longitude:F5}°";
    }

    public void UpdateDistance(double distance)
    {
        if (!double.IsNaN(_lastDist) && Math.Abs(_lastDist - distance) < 0.5) return;
        _lastDist = distance;

        if (_distanceLabel != null)
            _distanceLabel.Text = distance <= 5.0 ? "YOU ARE HERE" : FormatDistance(distance);
    }

    // ── Heritage info panel ───────────────────────────────────────────────────

    public void ShowHeritageInfo(string title, string description)
    {
        if (_infoPanel == null) return;
        bool alreadyVisible = _infoPanel.Visible && _shownSiteTitle == title;
        if (alreadyVisible) return;

        _shownSiteTitle = title;

        if (_infoPanelTitle != null)
            _infoPanelTitle.Text = title ?? string.Empty;

        if (_infoPanelDescription != null)
        {
            _infoPanelDescription.FitContent = true;
            _infoPanelDescription.Text = description ?? string.Empty;
        }

        if (!_infoPanel.Visible)
        {
            _infoPanel.Modulate = new Color(1, 1, 1, 0);
            _infoPanel.Show();
            var tween = _infoPanel.CreateTween();
            tween.TweenProperty(_infoPanel, "modulate:a", 1.0f, 0.25f);
        }
    }

    public void HideInfoPanel()
    {
        if (_infoPanel == null || !_infoPanel.Visible) return;
        _shownSiteTitle = null;
        _infoPanel.Hide();
    }

    // ── Navigation panels ─────────────────────────────────────────────────────

    public void TogglePlacesList()
    {
        if (_placesListUI != null)
            _placesListUI.Visible = !_placesListUI.Visible;
    }

    public void HidePlacesList()  => _placesListUI?.Hide();
    public void HideStartButton() => _startButton?.Hide();

    public void ToggleRadarVisibility()
    {
        if (_radarContainer != null)
            _radarContainer.Visible = !_radarContainer.Visible;
    }

    // ── Event wiring ──────────────────────────────────────────────────────────

    public void HookUiEvents(Action onStart, Action onLandmarkToggle, Action onMapToggle,
                             Action onClosePlaces, Action onSpanishGate)
    {
        if (_startButton  != null) _startButton.Pressed  += () => onStart?.Invoke();
        if (_landmarkBtn  != null) _landmarkBtn.Pressed  += () => onLandmarkToggle?.Invoke();
        if (_mapBtn       != null) _mapBtn.Pressed       += () => onMapToggle?.Invoke();
        if (_closePlacesBtn != null) _closePlacesBtn.Pressed += () => onClosePlaces?.Invoke();
        if (_spanishGateBtn != null) _spanishGateBtn.Pressed += () => onSpanishGate?.Invoke();
    }

    // ── Camera ────────────────────────────────────────────────────────────────

    public void ApplyCameraTexture(CameraTexture cameraTexture, Vector2 screenSize)
    {
        if (cameraTexture == null || _cameraDisplay == null) return;

        _cameraDisplay.Texture = cameraTexture;
        _cameraDisplay.Set("expand_mode", 1);
        _cameraDisplay.Set("stretch_mode", 6);
        _cameraDisplay.Set("mouse_filter", 2);
        _cameraDisplay.ZIndex = -1;

        // Android back camera sensor outputs landscape (90° off from portrait).
        // Swap W/H so after -90° rotation the rect fills the portrait viewport exactly.
        var rotatedSize = new Vector2(screenSize.Y, screenSize.X);
        var pivot       = rotatedSize / 2.0f;
        _cameraDisplay.Size        = rotatedSize;
        _cameraDisplay.PivotOffset = pivot;
        _cameraDisplay.Position    = screenSize / 2.0f - pivot;
        _cameraDisplay.Rotation    = Mathf.Pi / 2.0f;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string FormatDistance(double meters) =>
        meters >= 1000.0
            ? $"{meters / 1000.0:F1} km away"
            : $"{meters:F0} m away";
}
