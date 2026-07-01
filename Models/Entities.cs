namespace BloodConnect.Models;

public class Donor
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
    public BloodGroup BloodGroup { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string CityArea { get; set; } = "";
    public DateTime? LastDonationDate { get; set; }
    public bool IsAvailable { get; set; } = true;

    public bool IsEligible =>
        !LastDonationDate.HasValue || (DateTime.UtcNow - LastDonationDate.Value).TotalDays >= 90;
}

public enum UrgencyLevel { Normal, Urgent, Critical }
public enum RequestStatus { Open, Fulfilled, Expired }

public class BloodRequest
{
    public int Id { get; set; }
    public string PatientName { get; set; } = "";
    public string HospitalName { get; set; } = "";
    public string ContactPhone { get; set; } = "";
    public BloodGroup BloodGroupNeeded { get; set; }
    public int UnitsNeeded { get; set; } = 1;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string CityArea { get; set; } = "";
    public UrgencyLevel Urgency { get; set; } = UrgencyLevel.Normal;
    public RequestStatus Status { get; set; } = RequestStatus.Open;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum ResponseStatus { Notified, Accepted, Declined, Completed }

public class DonationResponse
{
    public int Id { get; set; }
    public int BloodRequestId { get; set; }
    public int DonorId { get; set; }
    public ResponseStatus Status { get; set; } = ResponseStatus.Notified;
    public DateTime NotifiedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RespondedAt { get; set; }
}

// DTO used to show a ranked match on screen
public class DonorMatch
{
    public Donor Donor { get; set; } = null!;
    public double DistanceKm { get; set; }
}
