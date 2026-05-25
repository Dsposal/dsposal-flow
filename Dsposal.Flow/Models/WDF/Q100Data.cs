using Dsposal.Flow.Extensions;

namespace Dsposal.Flow.Models.WDF;
public class Q100Data : SharedData
{
    public long WasteProcessorId { get; set; }
    public long WasteStreamId { get; set; }
    
    // SenderWasteProcessor should be the PREVIOUS WasteProcessorOutputId within the same WasteStream
    public long WasteProcessorOutputId { get; set; }
    public long SenderWasteProcessorOutputId { get; set; }
    
    public long AuthorityId { get; set; }
    
    public long WasteStreamTypeId { get; set; }
    public string WasteStreamType { get; set; }
    
    public long FacilityTypeId { get; set; }
    public string FacilityType { get; set; }
    
    public long NationalFacilityId { get; set; }
    public string FacilityName { get; set; }
    
    public bool IsExemptFacility => !string.IsNullOrEmpty(FacilityName) && FacilityName.Contains("Exempt", StringComparison.OrdinalIgnoreCase);
    public bool IsOutsideUKFacility => !string.IsNullOrEmpty(FacilityName) && FacilityName.Contains("Outside UK", StringComparison.OrdinalIgnoreCase);
    
    public bool SkipFacilityAddressNormalization => IsExemptFacility || IsOutsideUKFacility;
    
    // The NationalFacilityId _can_ be the same for DIFFERENT addresses (Other/Except for example).
    public string? FacilityAddress { get; set; }
    public string? FacilityPostCode { get; set; }
    
    public string? FacilityLicence { get; set; }
    public string? FacilityCode { get; set; }
    
    public long? OutputProcessTypeId { get; set; } // 0 = No output process?
    public string? OutputProcessType { get; set; }
    
    public double TotalTonnes { get; set; }
    
    public long MaterialId { get; set; } // 0 = No material?
    public string? Material { get; set; }
    
    public double TonnesByMaterial { get; set; }
    
    public double? TonnesFromHHSources { get; set; } // Household sources
    public double? TonnesFromCommercialSources { get; set; }
    public double? TonnesFromIndustrialSources { get; set; }
    public double? TonnesFromNonHHSources { get; set; }
    
    public double? TonnesFromWfHSources { get; set; } // WfH = Waste from households. This _should_ be the same as TonnesFromHHSources
    public double? TonnesFromWnfHSources { get; set; } // WnfH = Waste from non-households. This _should_ be the same as TonnesFromNonHHSources
    
    // Usage doesn't seem to be used?
    public int? UsageId { get; set; }
    public string? Usage { get; set; }
    
    public string? QuarterlyComments { get; set; }
    public string? MonthlyComments { get; set; }

    public Tuple<string[], string[]> GetPermitsAndRegistrations()
    {
        // We need to check FacilityLicense _AND_ FacilityCode for both permits.
        var permitsFromFacilityLicence = FacilityLicence?.ExtractPermits() ?? [];
        var registrationsFromFacilityLicence = FacilityLicence?.ExtractRegistrations() ?? [];

        var permitsFromFacilityCode = FacilityCode?.ExtractPermits() ?? [];
        var registrationsFromFacilityCode = FacilityCode?.ExtractRegistrations() ?? [];
        
        return new Tuple<string[], string[]>(permitsFromFacilityLicence.Concat(permitsFromFacilityCode).ToArray(), registrationsFromFacilityLicence.Concat(registrationsFromFacilityCode).ToArray());
    }
}