using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TOCC.IBE.Compare.Models.Common
{
    public enum AvailabilityProductTypes
    {
        SingleProduct,
        MultiProduct
    }

    public enum BuildLevels
    {
        Primary,
        Secondary,
        Tertiary
    }

    public enum PriceAdjustmentsSources
    {
        Promotion,
        Event,
        Rule
    }

    public enum DisplayModes
    {
        TravelPeriod,
        CheckInAndNights,
        Hourly
    }

    public enum ReservationRequestTypes
    {
        Rebook,
        Modify
    }

    public enum EventManagementEventSetStatusTypes
    {
        Draft,
        Accepted,
        Rejected,
        Closed
    }

    public enum EventManagementEventTypes
    {
        Wedding,
        Conference,
        Workshop,
        FamilyCelebration,
        CompanyCelebration,
        Birthday,
        Other
    }

    public enum FlexOccupancySearchModes
    {
        Maximum,
        Minimum,
        Double,
        FullFlex
    }

    public enum NavItemType
    {
        CustomLink,
        Route,
        DocumentLink,
    }

    public enum OutputModes
    {
        Calendar,
        Availability
    }

    public enum OutputTargetTypes
    {
        SingleProperty,
        Group
    }

    public enum PriceOutputTypes
    {
        Total,
        PerTick
    }

    public enum QueryParametersTypes
    {
        Default = 0,
        FlexOccupancy = 1,
        AlternativeOccupancy = 2,
        OtherDate = 3,
        AlternativeProperty = 4,
        AvailabilityCalendar = 5,
        WithoutFilters = 6,
        ParallelSets = 7
    }

    public enum RequesterTypes
    {
        Default,
        Legacy
    }

    public enum ScanActionModes
    {
        Group,
        Person
    }

    public enum ScanActions
    {
        CheckIn,
        CheckOut
    }

    public enum ScanDevices
    {
        App,
        Turnstile
    }

    public enum ShoppingCartRights
    {
        FullAccess,
        GuestsData,
        AddProducts
    }

    public enum ShoppingCartStatus
    {
        New,
        CheckedOut,
        Cancelled
    }

    public enum TagType
    {
        Default,
        Commercial,
        Visibility,
        Highlighting
    }

    public enum ThemeAttributeTypes
    {
        Font,
        PaletteColor,
        HexColor,
        Text,
        Value
    }

    public enum ThemeBackgroundStructures
    {
        Floral,
        Dune,
        Spots,
        Art,
        Noise,
        Flux,
        BlurredBlob,
        CustomImage,
        CustomVideo
    }

    public enum ThemeBackgroundTypes
    {
        Gradient,
        Structure
    }

    public enum ThemeColorPaletteGenerationModes
    {
        Raw,
        FromUrl,
        FromImage
    }

    public enum ThemeDarkmodePreference
    {
        Auto,
        Light,
        Dark,
    }

    public enum ThemeImageTypes
    {
        Logo,
        Primary,
        Secondary,
        Tertiary
    }

    public enum ThemeLayoutTypes
    {
        Fullscreen,
        Split,
        Circles,
        Simple,
    }

    public enum ThemeModes
    {
        Light,
        Dark
    }

    public enum ThemeTenant
    {
        LandingPage,
        General,
    }

    public enum ValidatedSetTypes
    {
        Cheapest,
        BestConstraints,
        MostFlexible,
        SameTariff,
        Raw,
        UnfulfillableGap,
        ForRecursion,
        Merged
    }

    public enum VoucherRestrictionValidationReason
    {
        None,
        MaxLos,
        MinLos,
        Weekday,
        NumberOfPersons,
        Redeemed,
        ExpireBeforeArrival,
        ExpireDuringPeriod,
        InvalidPeriod
    }

    public enum VoucherState
    {
        Unredeemed,
        Redeemed,
        Refunded,
        Deactivated,
        Authorized,
        Pending
    }

    public enum VoucherType
    {
        ValueVoucher,
        ProductVoucher,
        DynamicVoucher
    }

    public enum VoucherLogType
    {
        Redeem,
        Refund
    }

    public enum WidgetTypes
    {
        Quickbook,
        RoomList,
        PackageList,
        VoucherShop,
        AvailabilityCalendar,
        GapFinder
    }

    public enum CodeType
    {
        Rate,
        Promotion,
        Voucher,
    }

    public enum SettingVisibilityType
    {
        Mandatory,
        Optional,
        None
    }

    public enum LogVerbosity
    {
        None = 0,
        Minimal = 1,
        Trace = 2,
        Debug = 3
    }

    public enum ValidationModes
    {
        WithinPeriod,
        WholePeriod
    }
    public enum ProductPriceCalculationModes
    {
        PerTick = 0,
        PerPiece = 1,
        PerPieceAndTick = 2,
        PerPerson = 3,
        PerPersonAndTick = 4,
        Simple = 5
    }
    public enum TimeUnits
    {
        Seconds = 0,
        Minutes = 1,
        Hours = 2,
        Days = 3,
        Weeks = 4,
        Months = 5,
        Years = 6
    }
    public enum TariffPropertyTypes
    {
        GenericInformation,
        Board,
        IsD21Standard,
        IsPackageTourActive,
        PackageTourOperatorOid,
        PackageDuration,
        ExtensionNights,
        ArrivalDays,
        Categories
    }
    public enum ScheduleTypes
    {
        /// <summary>
        /// A product is bookable in the given period per tick, e.g. daily, hourly, every x minutes
        /// - .Tick of ProductSettings does apply
        /// - .Periods of ProductSettings is not mandatory
        /// </summary>
        PerTick,

        /// <summary>
        /// A product which is bookable in a given shift for the whole shift, tick length of booked time frame
        /// is taken from length of the shift
        /// - .Tick of ProductSettings does not apply
        /// - .Periods of ProductSettings is mandatory
        /// </summary>
        Shifts,

        /// <summary>
        /// GRID: just works like Shifts, but is rendered differently in the booking process and behaves a little bit 
        /// could be a recurring event as well
        /// - .Tick of ProductSettings does not apply
        /// - .Periods of ProductSettings is mandatory
        /// </summary>
        Events,

        /// <summary>
        /// A product which is valid for a longer period, e.g. a summer season ticket for the local swimming pool 
        /// just behaves like shift and event products but is rendered differently in cockpit calendar
        /// - .Tick of ProductSettings does not apply
        /// - .Periods of ProductSettings is mandatory
        /// </summary>
        Seasonal,
        Multi
    }

    public enum TariffVisibilityModes
    {
        Always,
        Login,
        Locked,
        Invisible,
        ForComparisonOnly
    }
    public enum CancellationFeeRelationTypes
    {
        // TODO: int for legacy-export
        None = -1,

        FirstNight = 0,
        WholeStay = 1,
        WholeService
    }

    public enum BoardTypes
    {
        Undefined = 0,
        None = 1,
        Breakfast = 2,
        HalfBoard = 3,
        FullBoard = 4,
        AllInclusive = 5,
        NotAvailable = 6,
        BreakfastBuffet = 7,
        Board75 = 8
    }

    public enum PublishStates
    {
        Published,
        Draft,
        Inactive,
        Deleted
    }

    public enum CloseOtherTariffsTypes
    {
        Always = 0,
        OnlyIfAvailable = 1,
        Never = 2
    }
}
