using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace TripPilot.Models
{
    // ==========================================
    // 1. ENUMS (Type-safe categories)
    // ==========================================
    public enum VehicleType
    {
        Sedan,
        SUV,
        Hatchback,
        Motorcycle,
        Minivan,
        PickupTruck
    }

    public enum FuelType
    {
        Petrol,
        Diesel,
        CNG,
        Electric
    }

    public enum StopCategory
    {
        Waypoint,
        FuelStation,
        Restaurant,
        TeaCoffeeShop,
        Hotel,
        TouristAttraction,
        RestArea
    }

    public enum ExpenseCategory
    {
        Fuel,
        Accommodation,
        FoodAndDrinks,
        ActivitiesAndTickets,
        TollsAndParking,
        Miscellaneous
    }

    public enum ChecklistCategory
    {
        EssentialsAndDocuments,
        ClothingAndGear,
        VehicleReadiness,
        Electronics,
        MedicalAndSafety,
        CustomToDo
    }

    // ==========================================
    // 2. OOP INTERFACE (Abstraction Contract)
    // ==========================================
    public interface IFuelCalculable
    {
        double CalculateCurrentRangeKm();
        double CalculateRequiredFuelLiters(double distanceKm);
        decimal CalculateEstimatedFuelCost(double distanceKm);
        bool IsRefuelRequired(double distanceKm, double safetyBufferKm = 40.0);
    }

    // ==========================================
    // 3. OOP ABSTRACT BASE CLASS (Inheritance)
    // ==========================================
    public abstract class BaseEntity
    {
        [Key]
        public int Id { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Virtual method that derived classes can override (Polymorphism)
        public virtual string GetSummaryLabel()
        {
            return $"{this.GetType().Name} #{Id}";
        }
    }

    // ==========================================
    // 4. MAIN TRIP ENTITY (Composition Root)
    // ==========================================
    public class Trip : BaseEntity
    {
        [Required(ErrorMessage = "Please enter a trip title.")]
        [StringLength(100)]
        [Display(Name = "Trip Name")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Starting location is required.")]
        [StringLength(120)]
        [Display(Name = "Starting Location")]
        public string StartLocation { get; set; } = string.Empty;

        [Required(ErrorMessage = "Destination is required.")]
        [StringLength(120)]
        [Display(Name = "Final Destination")]
        public string Destination { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Departure Date")]
        public DateTime DepartureDate { get; set; } = DateTime.Today.AddDays(1);

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Return Date")]
        public DateTime ReturnDate { get; set; } = DateTime.Today.AddDays(4);

        [Range(1, 50000, ErrorMessage = "Distance must be between 1 and 50,000 km.")]
        [Display(Name = "Total Route Distance (km)")]
        public double TotalDistanceKm { get; set; } = 250;

        [Range(0.1, 1000, ErrorMessage = "Estimated hours must be positive.")]
        [Display(Name = "Est. Driving Time (Hours)")]
        public double EstimatedDurationHours { get; set; } = 4.5;

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 10000000)]
        [Display(Name = "Overall Trip Budget")]
        public decimal OverallBudget { get; set; } = 15000m;

        [StringLength(500)]
        [Display(Name = "Trip Notes")]
        public string? Notes { get; set; }

        // OOP Composition Relationships (Navigation Properties)
        public VehicleProfile? Vehicle { get; set; }
        public ICollection<TripStop> Stops { get; set; } = new List<TripStop>();
        public ICollection<ExpenseItem> Expenses { get; set; } = new List<ExpenseItem>();
        public ICollection<ChecklistItem> ChecklistItems { get; set; } = new List<ChecklistItem>();

        // Encapsulated Domain Methods
        public int GetTripDurationDays()
        {
            var days = (ReturnDate.Date - DepartureDate.Date).Days + 1;
            return days > 0 ? days : 1;
        }

        public decimal GetTotalEstimatedExpenses() => Expenses.Sum(e => e.EstimatedAmount);
        public decimal GetTotalActualExpenses() => Expenses.Sum(e => e.ActualAmount);
        public decimal GetRemainingBudget() => OverallBudget - GetTotalActualExpenses();

        public override string GetSummaryLabel()
        {
            return $"{Title} ({StartLocation} → {Destination}, {TotalDistanceKm} km)";
        }
    }

    // ==========================================
    // 5. VEHICLE & FUEL CALCULATOR MODEL
    // ==========================================
    public class VehicleProfile : BaseEntity, IFuelCalculable
    {
        public int TripId { get; set; }
        public Trip? Trip { get; set; }

        [Required]
        [StringLength(80)]
        [Display(Name = "Vehicle Name / Model")]
        public string VehicleName { get; set; } = "Family SUV";

        [Required]
        [Display(Name = "Vehicle Type")]
        public VehicleType VehicleType { get; set; } = VehicleType.SUV;

        [Required]
        [Display(Name = "Fuel Type")]
        public FuelType FuelType { get; set; } = FuelType.Petrol;

        [Range(1, 200, ErrorMessage = "Mileage must be between 1 and 200 km/L.")]
        [Display(Name = "Mileage (km per Liter)")]
        public double MileageKmPerLiter { get; set; } = 15.0;

        [Range(2, 300, ErrorMessage = "Tank capacity must be between 2 and 300 Liters.")]
        [Display(Name = "Fuel Tank Capacity (Liters)")]
        public double TankCapacityLiters { get; set; } = 50.0;

        [Range(0, 300, ErrorMessage = "Current fuel must be 0 or greater.")]
        [Display(Name = "Current Fuel Level (Liters)")]
        public double CurrentFuelLiters { get; set; } = 20.0;

        [Column(TypeName = "decimal(18,2)")]
        [Range(1, 1000, ErrorMessage = "Enter a valid fuel price per liter.")]
        [Display(Name = "Fuel Price per Liter")]
        public decimal FuelPricePerLiter { get; set; } = 107.40m;

        public double CalculateCurrentRangeKm()
        {
            return Math.Round(CurrentFuelLiters * MileageKmPerLiter, 1);
        }

        public double CalculateFullTankRangeKm()
        {
            return Math.Round(TankCapacityLiters * MileageKmPerLiter, 1);
        }

        public double CalculateRequiredFuelLiters(double distanceKm)
        {
            if (MileageKmPerLiter <= 0) return 0;
            return Math.Round(distanceKm / MileageKmPerLiter, 2);
        }

        public double CalculateRemainingFuelAfterTripLiters(double distanceKm)
        {
            double used = CalculateRequiredFuelLiters(distanceKm);
            return Math.Round(CurrentFuelLiters - used, 2);
        }

        public decimal CalculateEstimatedFuelCost(double distanceKm)
        {
            double litersNeeded = CalculateRequiredFuelLiters(distanceKm);
            return Math.Round((decimal)litersNeeded * FuelPricePerLiter, 2);
        }

        public bool IsRefuelRequired(double distanceKm, double safetyBufferKm = 40.0)
        {
            double currentRange = CalculateCurrentRangeKm();
            return currentRange < (distanceKm + safetyBufferKm);
        }

        public override string GetSummaryLabel()
        {
            return $"{VehicleName} ({VehicleType} - {FuelType}, {MileageKmPerLiter} km/L)";
        }
    }

    // ==========================================
    // 6. ITINERARY STOP MODEL
    // ==========================================
    public class TripStop : BaseEntity
    {
        public int TripId { get; set; }
        public Trip? Trip { get; set; }

        [Required(ErrorMessage = "Stop name is required.")]
        [StringLength(120)]
        [Display(Name = "Stop / Place Name")]
        public string StopName { get; set; } = string.Empty;

        [Display(Name = "Stop Category")]
        public StopCategory Category { get; set; } = StopCategory.Waypoint;

        [Range(1, 100)]
        [Display(Name = "Stop Order")]
        public int SequenceOrder { get; set; } = 1;

        [Range(0, 50000)]
        [Display(Name = "Distance from Start (km)")]
        public double DistanceFromStartKm { get; set; }

        [Display(Name = "Planned Arrival Time")]
        public DateTime? PlannedArrival { get; set; }

        [StringLength(300)]
        [Display(Name = "Stop Notes")]
        public string? Notes { get; set; }

        public override string GetSummaryLabel()
        {
            return $"Stop #{SequenceOrder}: {StopName} ({Category})";
        }
    }

    // ==========================================
    // 7. BUDGET & EXPENSE ITEM MODEL
    // ==========================================
    public class ExpenseItem : BaseEntity
    {
        public int TripId { get; set; }
        public Trip? Trip { get; set; }

        [Required(ErrorMessage = "Expense title is required.")]
        [StringLength(120)]
        [Display(Name = "Expense Description")]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Category")]
        public ExpenseCategory Category { get; set; } = ExpenseCategory.Miscellaneous;

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 10000000)]
        [Display(Name = "Estimated Cost")]
        public decimal EstimatedAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 10000000)]
        [Display(Name = "Actual Spent")]
        public decimal ActualAmount { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Date Incurred")]
        public DateTime ExpenseDate { get; set; } = DateTime.Today;

        public decimal GetVariance() => EstimatedAmount - ActualAmount;
    }

    // ==========================================
    // 8. PACKING & TO-DO CHECKLIST MODEL
    // ==========================================
    public class ChecklistItem : BaseEntity
    {
        public int TripId { get; set; }
        public Trip? Trip { get; set; }

        [Required(ErrorMessage = "Checklist item text is required.")]
        [StringLength(150)]
        [Display(Name = "Item / Task")]
        public string ItemName { get; set; } = string.Empty;

        [Display(Name = "Category")]
        public ChecklistCategory Category { get; set; } = ChecklistCategory.EssentialsAndDocuments;

        [Display(Name = "Completed")]
        public bool IsCompleted { get; set; } = false;
    }
}