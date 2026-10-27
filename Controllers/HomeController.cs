using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TripPilot.Data;
using TripPilot.Models;

namespace TripPilot.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        // Dependency Injection: ASP.NET Core passes our SQL Server database context here
        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Load all saved trips with their related Vehicle, Stops, Expenses, and Checklists
            var trips = await _context.Trips
                .Include(t => t.Vehicle)
                .Include(t => t.Stops.OrderBy(s => s.SequenceOrder))
                .Include(t => t.Expenses)
                .Include(t => t.ChecklistItems)
                .OrderBy(t => t.DepartureDate)
                .ToListAsync();

            return View(trips);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}