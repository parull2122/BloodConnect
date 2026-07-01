using BloodConnect.Hubs;
using BloodConnect.Models;
using Microsoft.AspNetCore.SignalR;

namespace BloodConnect.Services;

/// <summary>
/// In-memory data store + matching engine for the demo.
/// Swap this out for an EF Core + SQL Server backed repository later —
/// the method signatures are written so that swap is a drop-in replacement.
/// </summary>
public class BloodBankService
{
    private readonly List<Donor> _donors = new();
    private readonly List<BloodRequest> _requests = new();
    private readonly List<DonationResponse> _responses = new();
    private readonly IHubContext<NotificationHub> _hub;

    private int _donorId = 1;
    private int _requestId = 1;
    private int _responseId = 1;

    // Blazor Server components subscribe to this to refresh their UI the instant
    // something changes elsewhere - this is what makes the dashboard feel "live"
    // across multiple open browser tabs, mirroring what the SignalR hub broadcasts
    // to any non-Blazor client.
    public event Action? OnChanged;

    public BloodBankService(IHubContext<NotificationHub> hub)
    {
        _hub = hub;
        SeedDemoData();
    }

    // ---------- Donor operations ----------

    public IReadOnlyList<Donor> GetAllDonors() => _donors.AsReadOnly();

    public Donor RegisterDonor(Donor donor)
    {
        donor.Id = _donorId++;
        _donors.Add(donor);
        return donor;
    }

    public void SetAvailability(int donorId, bool isAvailable)
    {
        var d = _donors.FirstOrDefault(x => x.Id == donorId);
        if (d != null) d.IsAvailable = isAvailable;
    }

    // ---------- Request operations ----------

    public IReadOnlyList<BloodRequest> GetActiveRequests() =>
        _requests.Where(r => r.Status == RequestStatus.Open)
                 .OrderByDescending(r => r.Urgency)
                 .ThenByDescending(r => r.CreatedAt)
                 .ToList();

    public async Task<(BloodRequest request, List<DonorMatch> matches)> CreateRequestAsync(BloodRequest request)
    {
        request.Id = _requestId++;
        _requests.Add(request);

        var matches = FindMatches(request);

        // Record a "Notified" response for each matched donor
        foreach (var m in matches)
        {
            _responses.Add(new DonationResponse
            {
                Id = _responseId++,
                BloodRequestId = request.Id,
                DonorId = m.Donor.Id,
                Status = ResponseStatus.Notified
            });
        }

        // Push a real-time notification to every connected client.
        // (In a full build, each donor would join a group and only matched donors get pinged.)
        await _hub.Clients.All.SendAsync("NewBloodRequest", new
        {
            request.Id,
            request.PatientName,
            request.HospitalName,
            BloodGroup = request.BloodGroupNeeded.ToLabel(),
            request.UnitsNeeded,
            Urgency = request.Urgency.ToString(),
            MatchedDonorIds = matches.Select(m => m.Donor.Id).ToArray()
        });

        OnChanged?.Invoke();
        return (request, matches);
    }

    public List<DonorMatch> FindMatches(BloodRequest request, double maxDistanceKm = 25)
    {
        return _donors
            .Where(d => d.IsAvailable && d.IsEligible)
            .Where(d => BloodGroupHelper.IsCompatible(d.BloodGroup, request.BloodGroupNeeded))
            .Select(d => new DonorMatch
            {
                Donor = d,
                DistanceKm = Haversine(request.Latitude, request.Longitude, d.Latitude, d.Longitude)
            })
            .Where(m => m.DistanceKm <= maxDistanceKm)
            .OrderBy(m => m.DistanceKm)
            .ToList();
    }

    // ---------- Response operations ----------

    public async Task RespondAsync(int requestId, int donorId, bool accepted)
    {
        var resp = _responses.FirstOrDefault(r => r.BloodRequestId == requestId && r.DonorId == donorId);
        if (resp == null) return;

        resp.Status = accepted ? ResponseStatus.Accepted : ResponseStatus.Declined;
        resp.RespondedAt = DateTime.UtcNow;

        var donor = _donors.First(d => d.Id == donorId);
        var request = _requests.First(r => r.Id == requestId);

        if (accepted)
        {
            request.Status = RequestStatus.Fulfilled;
            donor.LastDonationDate = DateTime.UtcNow;
        }

        await _hub.Clients.All.SendAsync("RequestResponded", new
        {
            requestId,
            donorId,
            DonorName = donor.Name,
            Accepted = accepted
        });

        OnChanged?.Invoke();
    }

    public List<DonationResponse> GetResponsesForRequest(int requestId) =>
        _responses.Where(r => r.BloodRequestId == requestId).ToList();

    // ---------- Utility ----------

    public static double Haversine(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371; // Earth radius km
        double dLat = ToRad(lat2 - lat1);
        double dLon = ToRad(lon2 - lon1);
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private static double ToRad(double deg) => deg * Math.PI / 180;

    private void SeedDemoData()
    {
        // A handful of donors spread around a city (roughly Indore coordinates)
        // so demo distances are realistic without needing map input.
        _donors.AddRange(new[]
        {
            new Donor { Id = _donorId++, Name = "Rahul Sharma", Phone = "9000000001", Email = "rahul@example.com",
                BloodGroup = BloodGroup.OPositive, Latitude = 22.7196, Longitude = 75.8577, CityArea = "Vijay Nagar",
                LastDonationDate = DateTime.UtcNow.AddDays(-120), IsAvailable = true },
            new Donor { Id = _donorId++, Name = "Priya Verma", Phone = "9000000002", Email = "priya@example.com",
                BloodGroup = BloodGroup.ONegative, Latitude = 22.7350, Longitude = 75.8900, CityArea = "Bhawarkuan",
                LastDonationDate = DateTime.UtcNow.AddDays(-40), IsAvailable = true },
            new Donor { Id = _donorId++, Name = "Aman Khan", Phone = "9000000003", Email = "aman@example.com",
                BloodGroup = BloodGroup.APositive, Latitude = 22.7000, Longitude = 75.8300, CityArea = "Rajwada",
                LastDonationDate = null, IsAvailable = true },
            new Donor { Id = _donorId++, Name = "Sneha Patil", Phone = "9000000004", Email = "sneha@example.com",
                BloodGroup = BloodGroup.BPositive, Latitude = 22.7500, Longitude = 75.9000, CityArea = "Palasia",
                LastDonationDate = DateTime.UtcNow.AddDays(-200), IsAvailable = true },
            new Donor { Id = _donorId++, Name = "Vikram Singh", Phone = "9000000005", Email = "vikram@example.com",
                BloodGroup = BloodGroup.ABPositive, Latitude = 22.6900, Longitude = 75.8800, CityArea = "MG Road",
                LastDonationDate = DateTime.UtcNow.AddDays(-10), IsAvailable = true },
            new Donor { Id = _donorId++, Name = "Neha Joshi", Phone = "9000000006", Email = "neha@example.com",
                BloodGroup = BloodGroup.ONegative, Latitude = 22.7700, Longitude = 75.9200, CityArea = "Bengali Square",
                LastDonationDate = DateTime.UtcNow.AddDays(-95), IsAvailable = true },
        });
    }
}
