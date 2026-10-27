using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TripPilot.Data;
using TripPilot.Models;

namespace TripPilot.Controllers
{
    public class TripsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TripsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Trips (List all saved trips)
        public async Task<IActionResult> Index()
        {
            var trips = await _context.Trips
                .Include(t => t.Vehicle)
                .Include(t => t.Stops.OrderBy(s => s.SequenceOrder))
                .Include(t => t.Expenses)
                .Include(t => t.ChecklistItems)
                .OrderBy(t => t.DepartureDate)
                .ToListAsync();

            return View(trips);
        }

        // GET: /Trips/Details/5 (Full Trip Workspace: Stops, Fuel, Budget, Checklist)
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var trip = await _context.Trips
                .Include(t => t.Vehicle)
                .Include(t => t.Stops.OrderBy(s => s.SequenceOrder))
                .Include(t => t.Expenses.OrderByDescending(e => e.ExpenseDate))
                .Include(t => t.ChecklistItems)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (trip == null) return NotFound();

            return View(trip);
        }

        // GET: /Trips/Create
        public IActionResult Create()
        {
            var defaultTrip = new Trip
            {
                DepartureDate = DateTime.Today.AddDays(1),
                ReturnDate = DateTime.Today.AddDays(3),
                TotalDistanceKm = 250,
                EstimatedDurationHours = 4.5,
                OverallBudget = 15000m,
                Vehicle = new VehicleProfile
                {
                    VehicleName = "Family Car",
                    VehicleType = VehicleType.SUV,
                    FuelType = FuelType.Petrol,
                    MileageKmPerLiter = 15.0,
                    TankCapacityLiters = 50.0,
                    CurrentFuelLiters = 20.0,
                    FuelPricePerLiter = 107.40m
                }
            };
            return View(defaultTrip);
        }

        // POST: /Trips/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Trip trip, VehicleProfile vehicle)
        {
            if (trip.ReturnDate < trip.DepartureDate)
            {
                ModelState.AddModelError("ReturnDate", "Return date cannot be earlier than departure date.");
            }

            if (ModelState.IsValid)
            {
                trip.Vehicle = vehicle;
                _context.Trips.Add(trip);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Trip '{trip.Title}' created! You can now add stops, expenses, and checklists.";
                return RedirectToAction(nameof(Details), new { id = trip.Id });
            }
            trip.Vehicle = vehicle;
            return View(trip);
        }

        // GET: /Trips/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var trip = await _context.Trips
                .Include(t => t.Vehicle)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (trip == null) return NotFound();

            return View(trip);
        }

        // POST: /Trips/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Trip updatedTrip)
        {
            if (id != updatedTrip.Id) return NotFound();

            var existingTrip = await _context.Trips.FirstOrDefaultAsync(t => t.Id == id);
            if (existingTrip == null) return NotFound();

            existingTrip.Title = updatedTrip.Title;
            existingTrip.StartLocation = updatedTrip.StartLocation;
            existingTrip.Destination = updatedTrip.Destination;
            existingTrip.DepartureDate = updatedTrip.DepartureDate;
            existingTrip.ReturnDate = updatedTrip.ReturnDate;
            existingTrip.TotalDistanceKm = updatedTrip.TotalDistanceKm;
            existingTrip.EstimatedDurationHours = updatedTrip.EstimatedDurationHours;
            existingTrip.OverallBudget = updatedTrip.OverallBudget;
            existingTrip.Notes = updatedTrip.Notes;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Trip details updated!";
            return RedirectToAction(nameof(Details), new { id = existingTrip.Id });
        }

        // POST: /Trips/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var trip = await _context.Trips.FindAsync(id);
            if (trip != null)
            {
                _context.Trips.Remove(trip);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Trip deleted.";
            }
            return RedirectToAction("Index", "Home");
        }

        // ======================================================
        // ITINERARY STOPS: Add, Reorder (Move Up/Down), Delete
        // ======================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddStop(int tripId, string stopName, StopCategory category, double distanceFromStartKm, string? notes)
        {
            if (!string.IsNullOrWhiteSpace(stopName))
            {
                var maxOrder = await _context.TripStops
                    .Where(s => s.TripId == tripId)
                    .Select(s => (int?)s.SequenceOrder)
                    .MaxAsync() ?? 0;

                var stop = new TripStop
                {
                    TripId = tripId,
                    StopName = stopName.Trim(),
                    Category = category,
                    SequenceOrder = maxOrder + 1,
                    DistanceFromStartKm = Math.Max(0, distanceFromStartKm),
                    Notes = notes
                };

                _context.TripStops.Add(stop);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Added stop '{stop.StopName}' to your route.";
            }
            return RedirectToAction(nameof(Details), new { id = tripId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MoveStop(int stopId, string direction)
        {
            var stop = await _context.TripStops.FindAsync(stopId);
            if (stop == null) return NotFound();

            var stops = await _context.TripStops
                .Where(s => s.TripId == stop.TripId)
                .OrderBy(s => s.SequenceOrder)
                .ToListAsync();

            int index = stops.FindIndex(s => s.Id == stopId);
            if (direction == "up" && index > 0)
            {
                int temp = stops[index].SequenceOrder;
                stops[index].SequenceOrder = stops[index - 1].SequenceOrder;
                stops[index - 1].SequenceOrder = temp;
                await _context.SaveChangesAsync();
            }
            else if (direction == "down" && index < stops.Count - 1)
            {
                int temp = stops[index].SequenceOrder;
                stops[index].SequenceOrder = stops[index + 1].SequenceOrder;
                stops[index + 1].SequenceOrder = temp;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Details), new { id = stop.TripId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteStop(int stopId)
        {
            var stop = await _context.TripStops.FindAsync(stopId);
            if (stop == null) return NotFound();

            int tripId = stop.TripId;
            _context.TripStops.Remove(stop);
            await _context.SaveChangesAsync();

            // Re-number remaining stops cleanly
            var remaining = await _context.TripStops
                .Where(s => s.TripId == tripId)
                .OrderBy(s => s.SequenceOrder)
                .ToListAsync();

            for (int i = 0; i < remaining.Count; i++)
            {
                remaining[i].SequenceOrder = i + 1;
            }
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = tripId });
        }

        // ======================================================
        // VEHICLE & FUEL CALCULATOR UPDATE
        // ======================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateVehicle(int tripId, string vehicleName, VehicleType vehicleType, FuelType fuelType,
            double mileageKmPerLiter, double tankCapacityLiters, double currentFuelLiters, decimal fuelPricePerLiter)
        {
            var vehicle = await _context.VehicleProfiles.FirstOrDefaultAsync(v => v.TripId == tripId);
            if (vehicle == null)
            {
                vehicle = new VehicleProfile { TripId = tripId };
                _context.VehicleProfiles.Add(vehicle);
            }

            vehicle.VehicleName = string.IsNullOrWhiteSpace(vehicleName) ? "My Vehicle" : vehicleName.Trim();
            vehicle.VehicleType = vehicleType;
            vehicle.FuelType = fuelType;
            vehicle.MileageKmPerLiter = Math.Max(1, mileageKmPerLiter);
            vehicle.TankCapacityLiters = Math.Max(2, tankCapacityLiters);
            vehicle.CurrentFuelLiters = Math.Clamp(currentFuelLiters, 0, vehicle.TankCapacityLiters);
            vehicle.FuelPricePerLiter = Math.Max(1, fuelPricePerLiter);

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Vehicle and fuel profile recalculated and saved!";
            return RedirectToAction(nameof(Details), new { id = tripId });
        }

        // ======================================================
        // BUDGET & EXPENSE MANAGEMENT
        // ======================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddExpense(int tripId, string title, ExpenseCategory category, decimal estimatedAmount, decimal actualAmount)
        {
            if (!string.IsNullOrWhiteSpace(title))
            {
                var expense = new ExpenseItem
                {
                    TripId = tripId,
                    Title = title.Trim(),
                    Category = category,
                    EstimatedAmount = Math.Max(0, estimatedAmount),
                    ActualAmount = Math.Max(0, actualAmount),
                    ExpenseDate = DateTime.Today
                };
                _context.ExpenseItems.Add(expense);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Expense '{expense.Title}' recorded.";
            }
            return RedirectToAction(nameof(Details), new { id = tripId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteExpense(int expenseId)
        {
            var expense = await _context.ExpenseItems.FindAsync(expenseId);
            if (expense == null) return NotFound();

            int tripId = expense.TripId;
            _context.ExpenseItems.Remove(expense);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = tripId });
        }

        // ======================================================
        // CHECKLIST & PACKING ITEMS
        // ======================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddChecklistItem(int tripId, string itemName, ChecklistCategory category)
        {
            if (!string.IsNullOrWhiteSpace(itemName))
            {
                var item = new ChecklistItem
                {
                    TripId = tripId,
                    ItemName = itemName.Trim(),
                    Category = category,
                    IsCompleted = false
                };
                _context.ChecklistItems.Add(item);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Details), new { id = tripId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleChecklistItem(int itemId)
        {
            var item = await _context.ChecklistItems.FindAsync(itemId);
            if (item == null) return NotFound();

            item.IsCompleted = !item.IsCompleted;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = item.TripId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteChecklistItem(int itemId)
        {
            var item = await _context.ChecklistItems.FindAsync(itemId);
            if (item == null) return NotFound();

            int tripId = item.TripId;
            _context.ChecklistItems.Remove(item);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = tripId });
        }

        // ======================================================
        // STANDALONE MODULES: Calculator, Discover, Alerts, Journey Mode
        // ======================================================
        public async Task<IActionResult> Calculator(int? tripId)
        {
            var trips = await _context.Trips.Include(t => t.Vehicle).ToListAsync();
            ViewBag.Trips = trips;

            var selectedTrip = tripId.HasValue
                ? trips.FirstOrDefault(t => t.Id == tripId.Value)
                : trips.FirstOrDefault();

            return View(selectedTrip);
        }

        public async Task<IActionResult> Discover(int? tripId)
        {
            var trips = await _context.Trips.Include(t => t.Stops).ToListAsync();
            ViewBag.Trips = trips;

            var selectedTrip = tripId.HasValue
                ? trips.FirstOrDefault(t => t.Id == tripId.Value)
                : trips.FirstOrDefault();

            return View(selectedTrip);
        }

        public async Task<IActionResult> Alerts(int? tripId)
        {
            var trips = await _context.Trips.ToListAsync();
            ViewBag.Trips = trips;

            var selectedTrip = tripId.HasValue
                ? trips.FirstOrDefault(t => t.Id == tripId.Value)
                : trips.FirstOrDefault();

            return View(selectedTrip);
        }

        public async Task<IActionResult> Journey(int? id)
        {
            var trip = id.HasValue
                ? await _context.Trips
                    .Include(t => t.Vehicle)
                    .Include(t => t.Stops.OrderBy(s => s.SequenceOrder))
                    .Include(t => t.Expenses)
                    .Include(t => t.ChecklistItems)
                    .FirstOrDefaultAsync(t => t.Id == id.Value)
                : await _context.Trips
                    .Include(t => t.Vehicle)
                    .Include(t => t.Stops.OrderBy(s => s.SequenceOrder))
                    .Include(t => t.Expenses)
                    .Include(t => t.ChecklistItems)
                    .FirstOrDefaultAsync();

            if (trip == null) return RedirectToAction(nameof(Create));

            return View(trip);
        }
    }
}