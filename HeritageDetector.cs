using System.Collections.Generic;

public class HeritageDetector
{
    private readonly List<HeritageSite> _sites;

    public HeritageDetector(IEnumerable<HeritageSite> sites)
    {
        _sites = new List<HeritageSite>(sites);
    }

    public HeritageSite? FindNearby(double latitude, double longitude)
    {
        foreach (var site in _sites)
        {
            if (GeoUtils.CalculateDistance(latitude, longitude, site.Latitude, site.Longitude) <= site.DetectionRadius)
                return site;
        }

        return null;
    }

    public HeritageSite? GetSiteByName(string name)
    {
        foreach (var site in _sites)
        {
            if (site.Name == name)
                return site;
        }

        return null;
    }
}
