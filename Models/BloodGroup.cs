namespace BloodConnect.Models;

public enum BloodGroup
{
    APositive, ANegative,
    BPositive, BNegative,
    ABPositive, ABNegative,
    OPositive, ONegative
}

public static class BloodGroupHelper
{
    public static string ToLabel(this BloodGroup bg) => bg switch
    {
        BloodGroup.APositive => "A+",
        BloodGroup.ANegative => "A-",
        BloodGroup.BPositive => "B+",
        BloodGroup.BNegative => "B-",
        BloodGroup.ABPositive => "AB+",
        BloodGroup.ABNegative => "AB-",
        BloodGroup.OPositive => "O+",
        BloodGroup.ONegative => "O-",
        _ => bg.ToString()
    };

    // Which donor groups can donate TO a given recipient group
    private static readonly Dictionary<BloodGroup, BloodGroup[]> CompatibleDonors = new()
    {
        [BloodGroup.OPositive] = new[] { BloodGroup.OPositive, BloodGroup.ONegative },
        [BloodGroup.ONegative] = new[] { BloodGroup.ONegative },
        [BloodGroup.APositive] = new[] { BloodGroup.APositive, BloodGroup.ANegative, BloodGroup.OPositive, BloodGroup.ONegative },
        [BloodGroup.ANegative] = new[] { BloodGroup.ANegative, BloodGroup.ONegative },
        [BloodGroup.BPositive] = new[] { BloodGroup.BPositive, BloodGroup.BNegative, BloodGroup.OPositive, BloodGroup.ONegative },
        [BloodGroup.BNegative] = new[] { BloodGroup.BNegative, BloodGroup.ONegative },
        [BloodGroup.ABPositive] = Enum.GetValues<BloodGroup>(), // universal recipient
        [BloodGroup.ABNegative] = new[] { BloodGroup.ABNegative, BloodGroup.ANegative, BloodGroup.BNegative, BloodGroup.ONegative },
    };

    public static bool IsCompatible(BloodGroup donor, BloodGroup recipientNeeds)
        => CompatibleDonors[recipientNeeds].Contains(donor);
}
