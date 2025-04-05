using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Secure_Student_Management_System.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Secure_Student_Management_System.Controllers
{
    [Authorize]
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ImageUploadService _imageUploadService;
        private readonly ILogger<StudentController> _logger;
        private const int PageSize = 10; // Pagination size

        public StudentController(ApplicationDbContext context, ImageUploadService imageUploadService, ILogger<StudentController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _imageUploadService = imageUploadService ?? throw new ArgumentNullException(nameof(imageUploadService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // ✅ Updated Index without ViewModel, using ViewData for pagination & search
        [Authorize(Roles = "Admin,User")]
        public async Task<IActionResult> Index(string? searchQuery, int pageNumber = 1)
        {
            searchQuery = searchQuery?.Trim().ToLower();
            var studentsQuery = _context.Students.AsQueryable();
            var currentUserId = User.Identity?.Name;

            // ✅ Users only see their own data
            if (User.IsInRole("User") && currentUserId != null)
            {
                studentsQuery = studentsQuery.Where(s => s.Id == currentUserId);
            }
            else if (!string.IsNullOrEmpty(searchQuery))
            {
                studentsQuery = studentsQuery.Where(s => s.FirstName.ToLower().Contains(searchQuery) ||
                                                          s.LastName.ToLower().Contains(searchQuery) ||
                                                          s.Id.ToLower().Contains(searchQuery));
            }

            int totalRecords = await studentsQuery.CountAsync();
            int totalPages = (int)Math.Ceiling((double)totalRecords / PageSize);

            var students = await studentsQuery
                .Skip((pageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            // ✅ Using ViewData to store pagination & search information
            ViewData["CurrentPage"] = pageNumber;
            ViewData["TotalPages"] = totalPages;
            ViewData["SearchQuery"] = searchQuery;

            return View(students); // Passing only Student list
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("FirstName,LastName,Email,MobileNumber,EnrolmentStatus")] Student student, IFormFile? profileImage)
        {
            if (student == null)
            {
                return BadRequest("Student object cannot be null.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (profileImage != null && profileImage.Length > 0)
                    {
                        var validationResult = ValidateImage(profileImage);
                        if (!validationResult.IsValid)
                        {
                            ModelState.AddModelError("ProfileImage", validationResult.ErrorMessage);
                            return View(student);
                        }

                        student.ProfileImageUrl = await _imageUploadService.UploadImageAsync(profileImage);
                    }

                    _context.Add(student);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating student.");
                    ModelState.AddModelError("", "An error occurred while saving the student.");
                }
            }
            return View(student);
        }

        [Authorize(Roles = "Admin,User")]
        public async Task<IActionResult> Edit(string? id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var student = await _context.Students.FindAsync(id);
            if (student == null)
            {
                return NotFound();
            }

            if (User.IsInRole("User") && student.Id != User.Identity?.Name)
            {
                return Unauthorized();
            }

            return View(student);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,User")]
        public async Task<IActionResult> Edit(string? id, [Bind("Id,FirstName,LastName,Email,MobileNumber,EnrolmentStatus,ProfileImageUrl")] Student student, IFormFile? profileImage)
        {
            if (string.IsNullOrEmpty(id) || id != student.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (profileImage != null && profileImage.Length > 0)
                    {
                        var validationResult = ValidateImage(profileImage);
                        if (!validationResult.IsValid)
                        {
                            ModelState.AddModelError("ProfileImage", validationResult.ErrorMessage);
                            return View(student);
                        }

                        student.ProfileImageUrl = await _imageUploadService.UploadImageAsync(profileImage);
                    }

                    if (User.IsInRole("User") && student.Id != User.Identity?.Name)
                    {
                        return Unauthorized();
                    }

                    _context.Update(student);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    _logger.LogError(ex, "Error updating student.");
                    if (!StudentExists(student.Id))
                    {
                        return NotFound();
                    }
                    throw;
                }

                return RedirectToAction(nameof(Index));
            }
            return View(student);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SoftDelete(string id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null)
            {
                return NotFound();
            }

            student.EnrolmentStatus = EnrolmentStatus.Inactive;
            _context.Update(student);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmation(string id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null)
            {
                return NotFound();
            }

            return View(student);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null)
            {
                return NotFound();
            }

            _context.Students.Remove(student);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private bool StudentExists(string id)
        {
            return _context.Students.Any(e => e.Id == id);
        }

        private (bool IsValid, string ErrorMessage) ValidateImage(IFormFile image)
        {
            string[] allowedExtensions = { ".jpg", ".jpeg", ".png" };
            string? fileExtension = Path.GetExtension(image.FileName)?.ToLower();

            if (fileExtension == null || !allowedExtensions.Contains(fileExtension))
            {
                return (false, "Only JPEG and PNG files are allowed.");
            }

            return (true, string.Empty);
        }
    }
}
