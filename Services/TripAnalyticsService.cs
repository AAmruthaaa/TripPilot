using System;
using System.Collections.Generic;
using System.Linq;
using TripPilot.Models;

namespace TripPilot.Services
{
    // =========================================================================
    // PWO CONCEPT 1: INTERFACE (Abstraction & Polymorphic Contract)
    // =========================================================================
    public interface IFuelCalculationStrategy
    {
        string EnergyUnitLabel { get; }
        string EfficiencyUnitLabel { get; }
        double CalculateEffectiveMileage(VehicleProfile vehicle);
        double CalculateSafeBufferKm(VehicleType vehicleType);
        string GetRefuelAdvice(VehicleProfile vehicle, double routeDistanceKm);
    }

    // =========================================================================
    // PWO CONCEPT 2: ABSTRACT BASE STRATEGY + INHERITANCE + POLYMORPHISM
    // =========================================================================
    public abstract class FuelCalculationStrategyBase : IFuelCalculationStrategy
    {
        public abstract string EnergyUnitLabel { get; }
        public abstract string EfficiencyUnitLabel { get; }

        // Virtual method that subclasses override (Runtime Polymorphism)
        public virtual double CalculateEffectiveMileage(VehicleProfile vehicle)
        {
            // Apply realistic highway load factor based on VehicleType
            double factor = vehicle.VehicleType switch
            {
                VehicleType.SUV => 0.95,
                VehicleType.Minivan => 0.92,
                VehicleType.PickupTruck => 0.90,
                VehicleType.Motorcycle => 1.05,
                _ => 1.0
            };
            return Math.Round(vehicle.MileageKmPerLiter * factor, 2);
        }

        public virtual double CalculateSafeBufferKm(VehicleType vehicleType)
        {
            return vehicleType == VehicleType.Motorcycle ? 25.0 : 45.0;
        }

        public abstract string GetRefuelAdvice(VehicleProfile vehicle, double routeDistanceKm);
    }

    // Derived Class 1: Petrol & Diesel Vehicles
    public class LiquidFuelStrategy : FuelCalculationStrategyBase
    {
        public override string EnergyUnitLabel => "Liters";
        public override string EfficiencyUnitLabel => "km/L";

        public override string GetRefuelAdvice(VehicleProfile vehicle, double routeDistanceKm)
        {
            double range = vehicle.CalculateCurrentRangeKm();
            double buffer = CalculateSafeBufferKm(vehicle.VehicleType);
            if (range < routeDistanceKm + buffer)
            {
                double stopBeforeKm = Math.Max(15, Math.Round(range - buffer, 0));
                return $"Plan a {vehicle.FuelType} station stop before the {stopBeforeKm} km mark (includes a {buffer} km highway reserve buffer).";
            }
            return $"Your current {vehicle.FuelType} level ({vehicle.CurrentFuelLiters} L) safely covers the {routeDistanceKm} km journey.";
        }
    }

    // Derived Class 2: CNG Vehicles
    public class CngFuelStrategy : FuelCalculationStrategyBase
    {
        public override string EnergyUnitLabel => "kg (CNG)";
        public override string EfficiencyUnitLabel => "km/kg";

        public override double CalculateSafeBufferKm(VehicleType vehicleType) => 55.0; // Higher buffer since highway CNG pumps are spaced farther apart

        public override string GetRefuelAdvice(VehicleProfile vehicle, double routeDistanceKm)
        {
            double range = vehicle.CalculateCurrentRangeKm();
            double buffer = CalculateSafeBufferKm(vehicle.VehicleType);
            if (range < routeDistanceKm + buffer)
            {
                return $"CNG Alert: Highway CNG stations are less frequent. Refill before {Math.Max(15, Math.Round(range - buffer, 0))} km or verify bi-fuel petrol backup.";
            }
            return $"Current CNG cylinder level covers the route with a {buffer} km safety reserve.";
        }
    }

    // Derived Class 3: Electric Vehicles (EV)
    public class ElectricVehicleStrategy : FuelCalculationStrategyBase
    {
        public override string EnergyUnitLabel => "kWh";
        public override string EfficiencyUnitLabel => "km/kWh";

        public override double CalculateSafeBufferKm(VehicleType vehicleType) => 60.0; // EV 20% battery protection buffer

        public override string GetRefuelAdvice(VehicleProfile vehicle, double routeDistanceKm)
        {
            double range = vehicle.CalculateCurrentRangeKm();
            double buffer = CalculateSafeBufferKm(vehicle.VehicleType);
            if (range < routeDistanceKm + buffer)
            {
                return $"EV Fast-Charge Required: Plan a DC fast-charging stop before {Math.Max(15, Math.Round(range - buffer, 0))} km to keep battery State-of-Charge above 15%.";
            }
            return $"EV battery range ({range} km) is sufficient for the {routeDistanceKm} km route without intermediate fast-charging.";
        }
    }

    // =========================================================================
    // PWO CONCEPT 3: ENCAPSULATED SERVICE WITH FACTORY / STRATEGY SELECTION
    // =========================================================================
    public class CategoryBreakdownItem
    {
        public string CategoryName { get; set; } = string.Empty;
        public decimal EstimatedTotal { get; set; }
        public decimal ActualTotal { get; set; }
        public int PercentageOfSpend { get; set; }
    }

    public interface ITripAnalyticsService
    {
        IFuelCalculationStrategy GetStrategy(FuelType fuelType);
        List<CategoryBreakdownItem> GetExpenseBreakdown(Trip trip);
    }

    public class TripAnalyticsService : ITripAnalyticsService
    {
        // Polymorphic Factory Method returning the right strategy object at runtime
        public IFuelCalculationStrategy GetStrategy(FuelType fuelType)
        {
            return fuelType switch
            {
                FuelType.CNG => new CngFuelStrategy(),
                FuelType.Electric => new ElectricVehicleStrategy(),
                _ => new LiquidFuelStrategy()
            };
        }

        public List<CategoryBreakdownItem> GetExpenseBreakdown(Trip trip)
        {
            var totalActual = trip.GetTotalActualExpenses();
            var grouped = trip.Expenses
                .GroupBy(e => e.Category)
                .Select(g => new CategoryBreakdownItem
                {
                    CategoryName = g.Key.ToString(),
                    EstimatedTotal = g.Sum(x => x.EstimatedAmount),
                    ActualTotal = g.Sum(x => x.ActualAmount),
                    PercentageOfSpend = totalActual > 0
                        ? (int)Math.Round((g.Sum(x => x.ActualAmount) / totalActual) * 100)
                        : 0
                })
                .OrderByDescending(x => x.ActualTotal)
                .ToList();

            return grouped;
        }
    }
}