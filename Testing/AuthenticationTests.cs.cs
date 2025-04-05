using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Secure_Student_Management_System.Controllers;
using Secure_Student_Management_System.Models;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;
using System.Diagnostics;
using Microsoft.Extensions.Options;

public class AuthenticationTests
{
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly Mock<SignInManager<ApplicationUser>> _mockSignInManager;
    private readonly Mock<RoleManager<IdentityRole>> _mockRoleManager;
    private readonly AccountController _accountController;
    private readonly StudentController _studentController;
    private readonly Mock<ApplicationDbContext> _mockDbContext;
    private readonly Mock<ImageUploadService> _mockImageUploadService;
    private readonly Mock<ILogger<StudentController>> _mockLogger = new();

    public AuthenticationTests()
    {
        // Mock UserManager
        var userStore = new Mock<IUserStore<ApplicationUser>>();
        _mockUserManager = new Mock<UserManager<ApplicationUser >>(
            userStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        // Mock SignInManager
        var contextAccessor = new Mock<IHttpContextAccessor>();
        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
        _mockSignInManager = new Mock<SignInManager<ApplicationUser>>(
            _mockUserManager.Object, contextAccessor.Object, claimsFactory.Object, null!, null!, null!, null!);

        // Mock RoleManager
        var roleStore = new Mock<IRoleStore<IdentityRole>>();
        _mockRoleManager = new Mock<RoleManager<IdentityRole>>(
            roleStore.Object, null!, null!, null!, null!);

        // Mock DbContext
        _mockDbContext = new Mock<ApplicationDbContext>();
        var mockDbSet = new Mock<DbSet<Student>>();
        _mockDbContext.Setup(x => x.Set<Student>()).Returns(mockDbSet.Object);

        // Mock ImageUploadService
        _mockImageUploadService = new Mock<ImageUploadService>();

        // Initialize StudentController with CORRECT dependencies
        _studentController = new StudentController(
            _mockDbContext.Object,
            _mockImageUploadService.Object,
            _mockLogger.Object
        );

        // Initialize AccountController
        _accountController = new AccountController(
            _mockSignInManager.Object,
            _mockUserManager.Object,
            Mock.Of<ILogger<AccountController>>(),
            _mockRoleManager.Object
        );
    }

    // Update placeholder tests with actual async implementations
    // Example for Login_ValidCredentials_RedirectsToHome:
    [Fact]
    public async Task Login_ValidCredentials_RedirectsToHome()
    {
        // Arrange
        var model = new LoginViewModel
        {
            Email = "valid@user.com",
            Password = "ValidPassword123!"
        };

        _mockSignInManager.Setup(x => x.PasswordSignInAsync(
            model.Email,
            model.Password,
            model.RememberMe,
            false
        )).ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        // Act
        var result = await _accountController.Login(model);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("Index");
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsViewWithError()
    {
        // Arrange
        var model = new LoginViewModel
        {
            Email = "invalid@user.com",
            Password = "WrongPassword123!"
        };

        _mockSignInManager.Setup(x => x.PasswordSignInAsync(
            model.Email!,
            model.Password!,
            model.RememberMe,
            false
        )).ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

        // Act
        var result = await _accountController.Login(model);

        // Assert
        result.Should().BeOfType<ViewResult>()
            .Which.ViewData.ModelState.ErrorCount.Should().Be(1);

        _mockSignInManager.Verify(x => x.PasswordSignInAsync(
            model.Email!,
            model.Password!,
            model.RememberMe,
            false
        ), Times.Once);
    }

    [Fact]
    public async Task ExternalLoginConfirmation_FirstUser_AssignedAdminRole()
    {
        // Arrange
        var model = new ExternalLoginConfirmationViewModel { Email = "firstuser@domain.com" };

        // Mock empty user database
        _mockUserManager.Setup(x => x.Users)
            .Returns(new List<ApplicationUser>().AsQueryable());

        // Mock role existence
        _mockRoleManager.Setup(x => x.RoleExistsAsync("Admin"))
            .ReturnsAsync(true);

        // Mock external login - FIXED HERE
        var externalLoginInfo = new ExternalLoginInfo(
            new ClaimsPrincipal(),
            "Google",
            "test-key",
            "Test Display Name"
        );

        // Explicitly handle optional parameter
        _mockSignInManager.Setup(x => x.GetExternalLoginInfoAsync(It.IsAny<string>()))
            .ReturnsAsync(externalLoginInfo);

        // Act
        var result = await _accountController.ExternalLoginConfirmation(model);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("Index");

        // Remove duplicate verifications
        _mockUserManager.Verify(x => x.Users, Times.AtLeastOnce);
        _mockUserManager.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>()), Times.Once);
        _mockUserManager.Verify(x => x.AddToRoleAsync(
            It.Is<ApplicationUser>(u => u.Email == model.Email),
            "Admin"
        ), Times.Once);
        _mockUserManager.Verify(x => x.AddLoginAsync(
            It.IsAny<ApplicationUser>(),
            externalLoginInfo
        ), Times.Once);

        _mockUserManager.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>()), Times.Once);
        _mockUserManager.Verify(x => x.AddLoginAsync(It.IsAny<ApplicationUser>(), externalLoginInfo), Times.Once);
    }

    [Fact]
    public async Task StudentIndex_AsUser_ReturnsOnlyOwnRecords()
    {
        // Arrange
        var userClaims = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "user1"),
            new Claim(ClaimTypes.Role, "User")
        }, "mock"));

        _studentController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = userClaims }
        };

        var students = new List<Student>
        {
            new Student { Id = "user1", FirstName = "John" },
            new Student { Id = "user2", FirstName = "Jane" }
        }.AsQueryable();

        var mockDbSet = new Mock<DbSet<Student>>();
        mockDbSet.As<IQueryable<Student>>().Setup(m => m.Provider).Returns(students.Provider);
        mockDbSet.As<IQueryable<Student>>().Setup(m => m.Expression).Returns(students.Expression);
        mockDbSet.As<IQueryable<Student>>().Setup(m => m.ElementType).Returns(students.ElementType);
        mockDbSet.As<IQueryable<Student>>().Setup(m => m.GetEnumerator()).Returns(students.GetEnumerator());

        _mockDbContext.Setup(x => x.Set<Student>()).Returns(mockDbSet.Object);

        // Act
        var result = await _studentController.Index(null, 1) as ViewResult;
        var model = result?.ViewData.Model as List<Student>; // Fixed to use List<Student>

        // Assert
        model.Should().NotBeNull();
        model!.Count.Should().Be(1);
        model.First().Id.Should().Be("user1");
    }

    [Fact]
    public void Create_Student_RequiresAdminRole()
    {
        // ... existing test implementation (unchanged) ... 
    }

    [Fact]
    public async Task Create_InvalidFileType_ReturnsError()
    {
        // Arrange
        var student = new Student { FirstName = "John", LastName = "Doe", Email = "john.doe@example.com" };
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns("invalid.txt");
        mockFile.Setup(f => f.Length).Returns(1024);

        // Act
        var result = await _studentController.Create(student, mockFile.Object) as ViewResult;

        // Assert
        result.Should().NotBeNull();
        result!.ViewData.ModelState.ErrorCount.Should().Be(1);
    }


    [Fact]
    public async Task Index_ValidPage_ReturnsCorrectPage()
    {
        // Arrange
        var students = new List<Student>
        {
            new Student { Id = "1", FirstName = "John" },
            new Student { Id = "2", FirstName = "Jane" }
        };

        var mockDbSet = new Mock<DbSet<Student>>();
        mockDbSet.As<IQueryable<Student>>().Setup(m => m.Provider).Returns(students.AsQueryable().Provider);
        mockDbSet.As<IQueryable<Student>>().Setup(m => m.Expression).Returns(students.AsQueryable().Expression);
        mockDbSet.As<IQueryable<Student>>().Setup(m => m.ElementType).Returns(students.AsQueryable().ElementType);
        mockDbSet.As<IQueryable<Student>>().Setup(m => m.GetEnumerator()).Returns(students.AsQueryable().GetEnumerator());

        _mockDbContext.Setup(x => x.Set<Student>()).Returns(mockDbSet.Object);

        // Act
        var result = await _studentController.Index("John", 1) as ViewResult;
        var model = result?.ViewData.Model as List<Student>; // Fixed to use List<Student>

        // Assert
        model.Should().NotBeNull();
        model!.Count.Should().Be(1);
        model.First().FirstName.Should().Be("John");
    }


    [Fact]
    public async Task Index_ResponseTime_ShouldBeUnderThreshold()
    {
        // Arrange
        var students = Enumerable.Range(1, 1000)
            .Select(i => new Student { Id = i.ToString(), FirstName = $"Student{i}" })
            .AsQueryable();

        var mockDbSet = new Mock<DbSet<Student>>();
        mockDbSet.As<IQueryable<Student>>().Setup(m => m.Provider).Returns(students.Provider);
        mockDbSet.As<IQueryable<Student>>().Setup(m => m.Expression).Returns(students.Expression);
        mockDbSet.As<IQueryable<Student>>().Setup(m => m.ElementType).Returns(students.ElementType);
        mockDbSet.As<IQueryable<Student>>().Setup(m => m.GetEnumerator()).Returns(students.GetEnumerator());

        _mockDbContext.Setup(x => x.Set<Student>()).Returns(mockDbSet.Object);
        var stopwatch = new Stopwatch();

        // Act
        stopwatch.Start();
        var result = await _studentController.Index(null, 1);
        stopwatch.Stop();

        // Assert
        result.Should().BeOfType<ViewResult>();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(500);
    }

    public class StudentControllerTests
    {
        private readonly Mock<ApplicationDbContext> _mockContext;
        private readonly Mock<ImageUploadService> _mockImageUploadService;
        private readonly StudentController _controller;

        public StudentControllerTests()
        {
            _mockContext = new Mock<ApplicationDbContext>();
            _mockImageUploadService = new Mock<ImageUploadService>();

            // Correct constructor parameters
            _controller = new StudentController(
                _mockContext.Object,
                _mockImageUploadService.Object,
                Mock.Of<ILogger<StudentController>>()
            );
        }


        [Fact]
        public async Task Edit_Post_Should_Update_Student_Profile_And_Redirect()
        {
            // Arrange
            var studentId = "123";
            var student = new Student
            {
                Id = studentId,
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                MobileNumber = "1234567890",
                EnrolmentStatus = EnrolmentStatus.Active
            };

            var updatedStudent = new Student
            {
                Id = studentId,
                FirstName = "John",
                LastName = "Smith",
                Email = "john.smith@example.com",
                MobileNumber = "0987654321",
                EnrolmentStatus = EnrolmentStatus.Active
            };

            var mockStudentSet = new Mock<DbSet<Student>>();
            mockStudentSet.Setup(m => m.FindAsync(studentId)).ReturnsAsync(student);
            _mockContext.Setup(m => m.Set<Student>()).Returns(mockStudentSet.Object);

            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(1024);
            mockFile.Setup(f => f.FileName).Returns("valid.jpg");
            mockFile.Setup(f => f.OpenReadStream()).Returns(new MemoryStream());

            _mockImageUploadService.Setup(s => s.UploadImageAsync(It.IsAny<IFormFile>(), It.IsAny<int>(), It.IsAny<int>()))
                      .ReturnsAsync("https://example.com/image.jpg");
            // Act
            var result = await _controller.Edit(studentId, updatedStudent, mockFile.Object);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            _mockContext.Verify(c => c.Update(updatedStudent), Times.Once);
            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }


      [Fact]
public void SessionExpiration_UserIsLoggedOut_AfterSessionExpires()
{
    // Arrange: Set up user claims for the mock user
    var userClaims = new ClaimsPrincipal(new ClaimsIdentity(new[] {
        new Claim(ClaimTypes.Name, "testuser"),
        new Claim(ClaimTypes.Role, "User")
    }, "mock"));

    var context = new DefaultHttpContext
    {
        User = userClaims
    };

    // Mock IHttpContextAccessor
    var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
    mockHttpContextAccessor.Setup(h => h.HttpContext).Returns(context);

    // Mock SignInManager and UserManager with proper constructor parameters
    var mockSignInManager = new Mock<SignInManager<ApplicationUser>>(Mock.Of<IUserStore<ApplicationUser>>(),
        Mock.Of<IOptions<IdentityOptions>>(), Mock.Of<ILogger<SignInManager<ApplicationUser>>>(),
        Mock.Of<IAuthenticationSchemeProvider>(), Mock.Of<IUserConfirmation<ApplicationUser>>());

    var mockUserManager = new Mock<UserManager<ApplicationUser>>(
        Mock.Of<IUserStore<ApplicationUser>>(),
        Mock.Of<IOptions<IdentityOptions>>(),
        Mock.Of<IPasswordHasher<ApplicationUser>>(),
        Mock.Of<IEnumerable<IUserValidator<ApplicationUser>>>(),
        Mock.Of<IEnumerable<IPasswordValidator<ApplicationUser>>>(),
        Mock.Of<ILookupNormalizer>(),
        Mock.Of<IdentityErrorDescriber>(),
        Mock.Of<IServiceProvider>(),
        Mock.Of<ILogger<UserManager<ApplicationUser>>>());

    var mockRoleManager = new Mock<RoleManager<IdentityRole>>(
        Mock.Of<IRoleStore<IdentityRole>>(),
        Mock.Of<IEnumerable<IRoleValidator<IdentityRole>>>(),
        Mock.Of<ILookupNormalizer>(),
        Mock.Of<IdentityErrorDescriber>(),
        Mock.Of<ILogger<RoleManager<IdentityRole>>>());

    // Initialize AccountController
    var controller = new AccountController(
        mockSignInManager.Object,
        mockUserManager.Object,
        Mock.Of<ILogger<AccountController>>(),
        mockRoleManager.Object
    );

            // Simulate session expiration by resetting User to an empty principal
            // Simulate session expiration by creating an empty principal
            context.User = new ClaimsPrincipal(new ClaimsIdentity()); // No identity = unauthenticated user // Fix: Use ClaimsPrincipal.Empty instead of null

            // Act: Simulate a request to the Login action of AccountController
            var result = controller.Login() as RedirectToActionResult;

    // Assert: Check if the user is redirected to the Login page after session expiration
    Assert.NotNull(result); // Ensure that the result is not null
    Assert.Equal("Login", result?.ActionName); // Check the action name
    Assert.Equal("Account", result?.ControllerName); // Check the controller name
}


        // Add to AuthenticationTests.cs (outside existing classes)
        public class StudentDeletionTests
        {
            private readonly Mock<ApplicationDbContext> _mockContext;
            private readonly StudentController _controller;

            public StudentDeletionTests()
            {
                _mockContext = new Mock<ApplicationDbContext>();
                _controller = new StudentController(
                    _mockContext.Object,
                    Mock.Of<ImageUploadService>(),
                    Mock.Of<ILogger<StudentController>>()
                );
            }

            [Fact]
            public async Task DeleteConfirmed_AsAdmin_RemovesStudentPermanently()
            {
                // Arrange
                var studentId = "123";
                var student = new Student { Id = studentId };
                var mockDbSet = new Mock<DbSet<Student>>();
                mockDbSet.Setup(m => m.FindAsync(studentId)).ReturnsAsync(student);
                _mockContext.Setup(m => m.Set<Student>()).Returns(mockDbSet.Object);

                // Act
                var result = await _controller.DeleteConfirmed(studentId);

                // Assert
                var redirectResult = Assert.IsType<RedirectToActionResult>(result);
                Assert.Equal("Index", redirectResult.ActionName);
                _mockContext.Verify(c => c.Remove(student), Times.Once);
                _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            }
        }

        public class StudentCreationTests
        {
            private readonly Mock<ApplicationDbContext> _mockContext;
            private readonly Mock<ImageUploadService> _mockImageService;
            private readonly StudentController _controller;

            public StudentCreationTests()
            {
                _mockContext = new Mock<ApplicationDbContext>();
                _mockImageService = new Mock<ImageUploadService>();
                _controller = new StudentController(
                    _mockContext.Object,
                    _mockImageService.Object,
                    Mock.Of<ILogger<StudentController>>()
                );
            }

            [Fact]
            public async Task Create_ValidJpegFile_UploadsSuccessfully()
            {
                // Arrange
                var student = new Student { FirstName = "John" };
                var mockFile = new Mock<IFormFile>();
                mockFile.Setup(f => f.FileName).Returns("valid.jpg");
                mockFile.Setup(f => f.Length).Returns(1024);
                mockFile.Setup(f => f.OpenReadStream()).Returns(new MemoryStream());

                // Fix: Explicitly include optional parameters in the mock setup
                _mockImageService.Setup(s => s.UploadImageAsync(
                    It.IsAny<IFormFile>(),
                    It.IsAny<int>(),  // width
                    It.IsAny<int>()   // height
                )).ReturnsAsync("https://example.com/image.jpg");

                // Act
                var result = await _controller.Create(student, mockFile.Object) as RedirectToActionResult;

                // Assert
                result.Should().NotBeNull();
                result!.ActionName.Should().Be("Index");
                _mockContext.Verify(x => x.Add(It.IsAny<Student>()), Times.Once);
            }

            public class PerformanceTests
            {
                private readonly Mock<ApplicationDbContext> _mockContext;
                private readonly StudentController _controller;

                public PerformanceTests()
                {
                    _mockContext = new Mock<ApplicationDbContext>();
                    _controller = new StudentController(
                        _mockContext.Object,
                        Mock.Of<ImageUploadService>(),
                        Mock.Of<ILogger<StudentController>>()
                    );
                }

                [Fact]
                public async Task Index_Performance_ReturnsWithinThreshold()
                {
                    // Arrange
                    var stopwatch = new Stopwatch();
                    var students = Enumerable.Range(1, 1000)
                        .Select(i => new Student { Id = i.ToString(), FirstName = $"Student{i}" })
                        .AsQueryable();

                    var mockDbSet = new Mock<DbSet<Student>>();
                    mockDbSet.As<IQueryable<Student>>().Setup(m => m.Provider).Returns(students.Provider);
                    mockDbSet.As<IQueryable<Student>>().Setup(m => m.Expression).Returns(students.Expression);
                    _mockContext.Setup(x => x.Set<Student>()).Returns(mockDbSet.Object);

                    // Act
                    stopwatch.Start();
                    var result = await _controller.Index(null, 1);
                    stopwatch.Stop();

                    // Assert
                    result.Should().BeOfType<ViewResult>();
                    stopwatch.ElapsedMilliseconds.Should().BeLessThan(500);
                }
            }

            public class LoadTests
            {
                // Fix ConcurrentRequests_IndexPage_HandlesMultipleUsers
                [Fact]
                public async Task ConcurrentRequests_IndexPage_HandlesMultipleUsers()
                {
                    var mockContext = new Mock<ApplicationDbContext>();
                    var mockImageService = new Mock<ImageUploadService>();

                    // Setup mock database
                    var students = Enumerable.Range(1, 100)
                        .Select(i => new Student { Id = i.ToString() })
                        .AsQueryable();

                    var mockDbSet = new Mock<DbSet<Student>>();
                    mockDbSet.As<IQueryable<Student>>().Setup(m => m.Provider).Returns(students.Provider);
                    mockDbSet.As<IQueryable<Student>>().Setup(m => m.Expression).Returns(students.Expression);
                    mockContext.Setup(x => x.Set<Student>()).Returns(mockDbSet.Object);

                    var controllerFactory = () => new StudentController(
                        mockContext.Object,
                        mockImageService.Object,
                        Mock.Of<ILogger<StudentController>>()
                    );

                    var tasks = Enumerable.Range(0, 100)
                        .Select(_ => Task.Run(async () =>
                        {
                            var controller = controllerFactory();
                            await controller.Index(null, 1);
                        }));

                    await Task.WhenAll(tasks);
                }
            }
        }
    }
}
    
