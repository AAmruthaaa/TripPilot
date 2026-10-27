using System.Runtime.InteropServices;
using Microsoft.EntityFrameworkCore;
using TripPilot.Data;
using TripPilot.Models;
using TripPilot.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Add MVC Controllers, Views, and Services
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<ITripAnalyticsService, TripAnalyticsService>();

// 2. Cross-Platform Database: Use SQLite on Linux/Render, SQL Server on Windows
var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
var dbProvider = builder.Configuration["DatabaseProvider"];

if (!isWindows || string.Equals(dbProvider, "Sqlite", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite("Data Source=TripPilot.db"));
}
else
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
}

var app = builder.Build();

// 3. Ensure database is created and seed initial road trip
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.EnsureCreated();

        if (!context.Trips.Any())
        {
            var sampleTrip = new Trip
            {
                Title = "Hyderabad to Hampi Heritage Road Trip",
                StartLocation = "Hyderabad, Telangana",
                Destination = "Hampi, Karnataka",
                DepartureDate = DateTime.Today.AddDays(3),
                ReturnDate = DateTime.Today.AddDays(6),
                TotalDistanceKm = 378,
                EstimatedDurationHours = 7.5,
                OverallBudget = 22000m,
                Notes = "Scenic highway drive via NH 44 and NH 167 with heritage stops and highway dhabas.",
                Vehicle = new VehicleProfile
                {
                    VehicleName = "Hyundai Creta SX",
                    VehicleType = VehicleType.SUV,
                    FuelType = FuelType.Petrol,
                    MileageKmPerLiter = 14.5,
                    TankCapacityLiters = 50,
                    CurrentFuelLiters = 22,
                    FuelPricePerLiter = 107.40m
                }
            };

            sampleTrip.Stops.Add(new TripStop
            {
                SequenceOrder = 1,
                StopName = "Jadcherla Highway Breakfast Stop",
                Category = StopCategory.Restaurant,
                DistanceFromStartKm = 88,
                Notes = "Morning filter coffee and idli break along NH 44."
            });
            sampleTrip.Stops.Add(new TripStop
            {
                SequenceOrder = 2,
                StopName = "Raichur IndianOil Fuel Station",
                Category = StopCategory.FuelStation,
                DistanceFromStartKm = 205,
                Notes = "Top up petrol tank before entering Karnataka state highway stretch."
            });
            sampleTrip.Stops.Add(new TripStop
            {
                SequenceOrder = 3,
                StopName = "Virupaksha Temple & Hampi Bazaar",
                Category = StopCategory.TouristAttraction,
                DistanceFromStartKm = 378,
                Notes = "Main heritage arrival point and check-in."
            });

            sampleTrip.Expenses.Add(new ExpenseItem
            {
                Title = "Round-Trip Petrol Estimate",
                Category = ExpenseCategory.Fuel,
                EstimatedAmount = 5600m,
                ActualAmount = 3000m,
                ExpenseDate = DateTime.Today
            });
            sampleTrip.Expenses.Add(new ExpenseItem
            {
                Title = "Heritage Resort Stay (3 Nights)",
                Category = ExpenseCategory.Accommodation,
                EstimatedAmount = 9500m,
                ActualAmount = 9500m,
                ExpenseDate = DateTime.Today
            });
            sampleTrip.Expenses.Add(new ExpenseItem
            {
                Title = "Highway Tolls (FASTag) & Monument Tickets",
                Category = ExpenseCategory.TollsAndParking,
                EstimatedAmount = 1800m,
                ActualAmount = 850m,
                ExpenseDate = DateTime.Today
            });

            sampleTrip.ChecklistItems.Add(new ChecklistItem
            {
                ItemName = "Verify FASTag balance & RC / Insurance papers",
                Category = ChecklistCategory.VehicleReadiness,
                IsCompleted = true
            });
            sampleTrip.ChecklistItems.Add(new ChecklistItem
            {
                ItemName = "Check tire pressure (including spare tire)",
                Category = ChecklistCategory.VehicleReadiness,
                IsCompleted = true
            });
            sampleTrip.ChecklistItems.Add(new ChecklistItem
            {
                ItemName = "Pack power bank, car phone mount, and offline maps",
                Category = ChecklistCategory.Electronics,
                IsCompleted = false
            });

            context.Trips.Add(sampleTrip);
            context.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while initializing the database.");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
