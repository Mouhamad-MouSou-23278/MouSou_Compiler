using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OnlineCompiler.Application.Common.Interfaces;
using OnlineCompiler.Domain.Entities;

namespace OnlineCompiler.Infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task InitializeAsync(
        OnlineCompilerDbContext context,
        IPasswordHasher passwordHasher,
        ILogger logger)
    {
        try
        {
            await context.Database.EnsureCreatedAsync();

            if (!context.Users.Any())
            {
                logger.LogInformation("Seeding initial administrator and demo projects...");

                var adminUser = new User
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Email = "admin@mousou.dev",
                    FirstName = "Mouhamad",
                    LastName = "MouSou",
                    PasswordHash = passwordHasher.HashPassword("Admin123!"),
                    Role = UserRole.Admin,
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var demoProject = new Project
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    UserId = adminUser.Id,
                    Name = "Python Fibonacci & Data Processor",
                    Description = "Demonstrates multi-file imports, performance benchmarks, and real-time streaming output.",
                    Language = "python",
                    IsArchived = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var mainFile = new ProjectFile
                {
                    Id = Guid.NewGuid(),
                    ProjectId = demoProject.Id,
                    Path = "main.py",
                    Content = "# Python Data Processor Demo\nfrom math_utils import fibonacci\nimport time\n\ndef main():\n    print(\"--- Online Compiler V2 Execution Started ---\")\n    for n in range(1, 11):\n        print(f\"Fibonacci({n}) = {fibonacci(n)}\")\n        time.sleep(0.05)\n    print(\"--- Execution Finished Successfully ---\")\n\nif __name__ == \"__main__\":\n    main()\n",
                    IsEntryPoint = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var helperFile = new ProjectFile
                {
                    Id = Guid.NewGuid(),
                    ProjectId = demoProject.Id,
                    Path = "math_utils.py",
                    Content = "def fibonacci(n: int) -> int:\n    if n <= 0:\n        return 0\n    elif n == 1:\n        return 1\n    a, b = 0, 1\n    for _ in range(2, n + 1):\n        a, b = b, a + b\n    return b\n",
                    IsEntryPoint = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                context.Users.Add(adminUser);
                context.Projects.Add(demoProject);
                context.ProjectFiles.AddRange(mainFile, helperFile);

                await context.SaveChangesAsync();
                logger.LogInformation("Database seeded successfully with demo user: admin@mousou.dev (Password: Admin123!)");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database.");
        }
    }
}
