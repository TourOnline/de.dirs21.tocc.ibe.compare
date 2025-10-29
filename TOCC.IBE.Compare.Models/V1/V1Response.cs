using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using TOCC.Core.Extensions;
using TOCC.Core.Interfaces;
using TOCC.Core.Models.CodeGeneration;
using TOCC.IBE.Compare.Models.Attributes;
using TOCC.IBE.Compare.Models.Common;
using TOCC.IBE.Compare.Models.Core;

namespace TOCC.IBE.Compare.Models.V1
{
    public class V1Response
    {
        [SkipValidation]
        public Guid _uuid { set; get; } = Guid.NewGuid();
        public Info Info { set; get; }
        public Result Result { set; get; }
        public QueryParametersTypes Type { set; get; }

        [SkipValidation]
        public object Data { set; get; }

        [SkipValidation]
        public bool Explain { set; get; }

        [SkipValidation]
        public object IgnoreSteps { set; get; }

        public List<QueryParameters> AlternativeQueries { set; get; }
    }

    public class MainTariffInfo
    {
        public long? _id { get; set; }
        public string _LegacyId { get; set; }
        public string _RepositoryName { set; get; }
        public Guid _uuid { get; set; }
    }

    public class BaseOffer
    {
        public Guid? CancellationTerms_uuid { set; get; }
        public int? Capacity { set; get; }
        public ConstraintsInfo Constraints { set; get; }

        [SkipValidation]
        public BaseOfferDiscount Discount { set; get; }
        public BaseOfferFacts Facts { set; get; }
        public Guid? GuaranteePaymentTerms_uuid { set; get; }
        public bool HasConstraints { set; get; }
        public bool IsAvailable { set; get; }
        public bool IsValid { set; get; }

        [SkipValidation]
        public int Los { set; get; }

        [SkipValidation]
        public MainTariffInfo MainTariff { set; get; }

        public Guid MainTariff_uuid { set; get; }

        [SkipValidation]
        public string Name { get; set; }

        [CustomCompare(typeof(PriceComparer))]
        public PriceInfoType Price { set; get; }

        [SkipValidation]
        public PriceInfoType PriceNet { set; get; }
        public ValidatedSetTypes? SubType { set; get; }
        public List<Tag> Tags { set; get; } = new List<Tag>();
        public List<BaseOfferTaxes> Taxes { set; get; }

        [CustomCompare(typeof(OfferTickComparer))]
        public List<OfferTicks> Ticks { set; get; }
        public ValidatedSetTypes Type { get; set; }

        public class BaseOfferDiscount
        {
            public BaseOfferDiscountPrice After { set; get; }
            public List<Guid> AppliedPromotions { set; get; }
            public BaseOfferDiscountPrice Before { set; get; }
            public ValueTypes DisplayAs { set; get; } = ValueTypes.Absolute;
            public Ident Referenced { set; get; }

            public class BaseOfferDiscountPrice
            {
                public decimal PerTick { set; get; }
                public decimal Total { set; get; }
            }
        }

        public class BaseOfferFacts
        {
            public BoardTypes? BoardType { set; get; }
            public BaseOfferFactsCancellation Cancellation { set; get; }
            public List<CustomFact> Custom { get; set; }
            public BaseOfferFactsGuaranteePayment GuaranteePayment { set; get; }
            public IList<BaseOfferFactsIncludedServices> IncludedServices { set; get; }
            public bool? IsExemptFromCancellationFees { set; get; }
            public bool? IsFree { set; get; }
            public bool? IsLocked { set; get; }
            public BaseOfferFactsModification Modification { set; get; }
            public TariffVisibilityModes? Visibility { set; get; }



            public class BaseOfferFactsCancellation
            {
                public Guid? _uuid { get; set; }
                public IList<BaseOfferFactsCancellationFees> Fees { set; get; }

                public class BaseOfferFactsCancellationFees
                {
                    [CustomCompare(typeof(DateTimePrecisionComparer), 60.0)]
                    public DateTime DueDate { set; get; }
                    public int ReferenceTime { set; get; }
                    public CancellationFeeRelationTypes? RelatedTo { set; get; }
                    public int TotalDays { set; get; }
                    public decimal Value { set; get; }
                    public double? ValueRelative { set; get; }
                }
            }

            public class BaseOfferFactsGuaranteePayment
            {
                public Guid? _uuid { get; set; }
                public bool IsCreditCardNeeded { get; set; }
                public bool IsWholeAmountRequired { set; get; }
                public IList<string> PaymentMethods { set; get; }
                public IList<BaseOfferFactsGuaranteePaymentSplits> Splits { set; get; }

                public class BaseOfferFactsGuaranteePaymentSplits
                {
                    public decimal? AbsoluteValue { set; get; }

                    [CustomCompare(typeof(DateTimePrecisionComparer), 60.0)]
                    public DateTime DueDate { set; get; }
                    public int? RelativeValue { set; get; }
                }
            }

            public class BaseOfferFactsIncludedServices
            {
                public string _RepositoryName { set; get; }
                public Guid? _uuid { set; get; }
                public string Value { set; get; }
            }

            public class BaseOfferFactsModification
            {
                public BaseOfferFactsModification(DateTime deadline, int times)
                {
                    Deadline = deadline;
                    Times = times;
                }

                public DateTime Deadline { get; set; }
                public int Times { get; set; }
            }

            public class CustomFact
            {
                public string Icon { get; set; }
                public string Text { get; set; }
            }
        }

        public class BaseOfferTaxes
        {
            public bool IsInclusive { set; get; }
            public decimal PerPerson { set; get; }
            public decimal PerTick { set; get; }
            public Guid TaxGroup { set; get; }
            public decimal Total { set; get; }
        }

        public class ConstraintsInfo
        {
            public int? Capacity { set; get; }
            public bool? IsCta { get; set; }
            public bool? IsCtd { get; set; }
            public bool? IsOnlyCheckout { set; get; }
            public bool? IsUnfulfillable { set; get; }
            public int? MaxLos { get; set; }
            public int? MaxNumberOfPersons { set; get; }
            public int? MinLos { get; set; }
        }
    }

    public class OfferTicks
    {
        public int? Cooldown { set; get; }
        public DateTime From { set; get; }
        public bool? IsDependency { set; get; }
        public string Name { set; get; }
        public int NumberOfTicks { set; get; }
        public List<PersonPrice> PersonPrices { set; get; }
        public PriceObsolet Price { set; get; }
        public string Product_Name { set; get; }
        public Guid? Product_uuid { set; get; }
        public Guid Tariff_uuid { set; get; }
        public TickInfoType TickInfo { set; get; }
        public DateTime Until { set; get; }
    }

    public class TickInfoType
    {
        public bool IsExtensionNight { set; get; }
    }

    public enum ValueTypes
    {
        None = -1,
        Absolute = 0,
        Relative = 1,
        AbsoluteOnlyIfLessOrEqual = 2,
        Subtractive = 3,
        SubtractiveMinimumZero = 4,
        AbsolutePerPerson = 5,
        AbsolutePerRoom = 6,
        CpaAbsolute = 7,
        CpaRelative = 8
    }


    public class Info
    {
        public Dictionary<long, DataAlternativePropertyData> AlternativeProperties { set; get; }
        public IDictionary<Guid, object> ProductGroups { set; get; }
        public IDictionary<Guid, IBEProductViewModel> Products { set; get; }
        public IDictionary<Guid, object> ProductUnits { set; get; }
        public IDictionary<Guid, object> Promotions { set; get; }
        public IDictionary<long, IBEProductViewModel> Properties { set; get; }
        public IDictionary<Guid, object> SeatingLayouts { set; get; }
        public IDictionary<Guid, IBETariffViewModel> Tariffs { set; get; }
        public IDictionary<Guid, object> TaxGroups { set; get; }
        public IDictionary<Guid, CancellationTermInfo> CancellationTerms { set; get; }
        public IDictionary<Guid, object> VoucherPrograms { get; set; }
        public IDictionary<Guid, object> InfoPoints { get; set; }
    }

    public class CancellationTermInfo
    {
        public Guid _uuid { get; set; }
        public long _id { get; set; }
        public string Name { get; set; } = string.Empty;

        [CustomCompare(typeof(ObjectToStringComparer))]
        public object Terms { get; set; }
    }

    public class IBEProductAddress
    {
        public string Street { get; set; }
        public string StreetNumber { get; set; }
        public string Building { get; set; }
        public string FloorLevel { get; set; }
        public string City { get; set; }
        public string PostalCode { get; set; }
        public string StateProvince { get; set; }
        public string Country { get; set; }
        public string ISO_3166_1_Alpha2Code { get; set; }
        public string FormattedAddress { get; set; }

        [SkipValidation]
        public object Coordinate { get; set; }
    }

    public class IBEProductViewModel
    {
        public long _id { get; set; }
        public long _oid { get; set; }
        public Guid? _uuid { get; set; }
        public string _LegacyId { get; set; }
        public string _RepositoryName { get; set; }
        public IBEProductAddress Address { get; set; }
        public string Category { get; set; }
        public ProductViewModelContent Content { get; set; }
        public ProductViewModelFacts Facts { get; set; }
        public IBEViewModelMedia Media { get; set; }
        public ProductViewModelRestrictions Restrictions { get; set; }

        // TO DO:  change it later 
        [SkipValidation]
        public IList<object> Tags { get; set; }
        public object Group { get; set; }
        public object SeatingLayouts { get; set; }
        public string? Type { get; set; }
        public List<Guid> Units { get; set; }
        public ProductViewModelSettings Settings { get; set; }
        public ProductViewModelStats Stats { get; set; }
        public ProductViewModelSelling _Selling { get; set; }
        public PropertyViewModelSelling Selling { get; set; }
        public class ProductViewModelFacts
        {
            public bool? AreTicksSelectable { set; get; }
            public ProductPriceCalculationModes? CalculationMode { get; set; }
            public bool? IsFree { set; get; }
            public bool? IsLimitedByBookedPersons { set; get; }
            public bool? IsOccupancySelectable { set; get; }
            public bool? IsOnRequest { set; get; }

            public ProductViewModelFacts() { }
        }

        public class ProductViewModelStats
        {
            public double? MinPrice { get; set; }
            public double? AvgPrice { get; set; }
            public double? MaxPrice { get; set; }
        }

        public class ProductViewModelSettings
        {
            public Duration Tick { get; set; }
            public bool IsHourly { get; set; }
            public ScheduleTypes? ScheduleType { get; set; }
        }

        public class ProductViewModelRestrictions
        {
            public ProductViewModelRestrictionsMinMax Occupancy { get; set; }
            public ProductViewModelRestrictionsMinMax Quantity { get; set; }

            public class ProductViewModelRestrictionsMinMax
            {
                public int? Max { get; set; }
                public int? Min { get; set; }
                public int? Default { get; set; }
            }
        }

        public class ProductViewModelContent
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public string ShortName { get; set; }
            public bool? HasShortName { get; set; }
            public IList<object> InfoBlocks { get; set; }
        }

        public class ProductViewModelSelling
        {
            public ProductViewModelSellingDerived Derived { get; set; }
            public class ProductViewModelSellingDerived
            {
                public Ident Reference { get; set; }
            }
        }

        public class PropertyViewModelSelling
        {
            public string Currency { get; set; }
            public double ExchangeRate { get; set; }
        }
    }

    public class IBEViewModelMedia
    {
        public IEnumerable<ViewModelMediaImage> Images { get; set; }
        public IEnumerable<ViewModelMediaFile> Files { get; set; }
        public class ViewModelMediaImage
        {
            public string Category { get; set; }
            public ViewModelMediaImageContent Content { get; set; }
            public ViewModelMediaImageURI URI { get; set; }

            public class ViewModelMediaImageURI
            {
                public string Public { get; set; }
                public string Original { get; set; }
            }

            public class ViewModelMediaImageContent
            {
                public string Description { get; set; }
            }
        }

        public class ViewModelMediaFile
        {
            public string Path { get; set; }
            public string Text { get; set; }
        }
    }


    public partial class TariffProperties
    {
        public virtual System.Int64? _id { set; get; }
        public virtual System.String Key { set; get; }
        public virtual System.String Name { set; get; }
        public TariffPropertyTypes? Type { set; get; }
        public virtual System.String Value { set; get; }

    }

    public class IBETariffViewModel
    {
        public long _id { get; set; }
        public long _oid { get; set; }
        public string _RepositoryName { get; set; }
        public Guid _uuid { get; set; }
        public string _LegacyId { get; set; }
        public TariffViewModelContent Content { get; set; }
        public IBEViewModelMedia Media { get; set; }
        public IList<TariffProperties> Properties { get; set; }
        public TariffSelling Selling { get; set; }
        public TariffSettings Settings { get; set; }
        public TariffRestrictions Restrictions { get; set; }

        public class TariffViewModelContent
        {
            public string Name { get; set; }
            public string Description { get; set; }
        }
    }

    public class TariffSettings : TenantSettings
    {
        public bool? IsCodeProtected { get; set; }
        public bool IsExclusive { get; set; }
        public bool? IsMixable { get; set; }
        public CloseOtherTariffsTypes? CloseOtherTariffs { get; set; }
        public TariffVisibilityModes? VisibilityMode { get; set; }
    }

    public partial class TariffRestrictions
    {
        public virtual TariffRestrictionsAge Age { set; get; }
        public virtual TariffRestrictionsOccupancy Occupancy { set; get; }
    }

    public partial class TariffRestrictionsOccupancy
    {
        public System.Int32? Max { set; get; }
        public System.Int32? Min { set; get; }
    }

    public class TariffRestrictionsAge
    {
        public System.Int32? Max { set; get; }
        public System.Int32? Min { set; get; }
    }

    public abstract class TenantSettings
    {
        [SkipValidation]
        public object TimeFrames { set; get; }
        public BetterCollection<string> Attributes { get; set; }
        public bool IsCrossPeriodBookable { get; set; } = true;
        public bool IsCrossTimeFrameBookable { get; set; } = true;
        public TenantSettingsDependencies Dependencies { get; set; }

        [SkipValidation]
        public IList<object> Periods { set; get; }
        public TenantSettingsPricing Pricing { set; get; }
        public TenantSettingsRestrictions Restrictions { set; get; }
        public ScheduleTypes? ScheduleType { get; set; }
        public string Type { get; set; }
        public PublishStates? Status { get; set; }
        public Duration Tick { set; get; }
        public TimeSpan TickLength => (Tick?.AsTickLength()).GetValueOrDefault(TimeSpan.FromDays(1));

        public class TenantSettingsAttributes
        {
            public int? MinLos { get; set; }
        }

        public class TenantSettingsDependencies
        {
            public IList<Guid> Products { get; set; }
        }

        public class TenantSettingsPricing
        {
            public bool HasAgeRanges { get; set; } = false;
            public bool HasPersonRanges { get; set; } = false;
            public bool HasTickRanges { get; set; } = false;
        }

        public class TenantSettingsRestrictions
        {
            public bool IsBookableAccrossMultiplePeriods { get; set; }
            public bool IsBookableAccrossMultipleTimeFrames { get; set; }
        }

        public class TenantSettingsTimeFrame
        {
            public Guid? _uuid { get; set; }
            public int End { get; set; }
            public int? EveryXthOfMonth { set; get; }
            public DateTime? From { get; set; }
            public int Start { get; set; }
            public DateTime? Until { get; set; }

        }
    }

    public partial class TariffSelling
    {
        public virtual BoardTypes? BoardType { set; get; }
        public virtual TariffSellingDiscount Discount { set; get; }
        public virtual IList<TariffSellingIncludedServices> IncludedServices { set; get; }
        public virtual Ident OffsetReference { set; get; }


        public partial class TariffSellingDiscount
        {
            public virtual BetterCollection<System.Guid> Channels { set; get; }
            public virtual System.Boolean IsActive { set; get; }
            public virtual System.Boolean IsRelative { set; get; }

        }

        public partial class TariffSellingIncludedServices
        {
            public virtual System.String _RepositoryName { set; get; }
            public virtual System.Guid? _uuid { set; get; }
            public virtual System.Double? Quantity { set; get; }
            public virtual System.String Value { set; get; }
        }
    }


    public class DataAlternativePropertyData
    {
        public long _id { set; get; }
        public Guid Channel { set; get; }
        public string Directory { set; get; }
        public decimal Distance { set; get; }

        public bool ShouldSerialize_id()
        {
            return false;
        }
    }
}