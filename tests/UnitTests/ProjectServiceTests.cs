using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OnlineCompiler.Application.DTOs.Projects;
using OnlineCompiler.Application.Services;
using OnlineCompiler.Infrastructure.Persistence;
using Xunit;

namespace OnlineCompiler.UnitTests;

public class ProjectServiceTests
{
    private OnlineCompilerDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<OnlineCompilerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new OnlineCompilerDbContext(options);
    }

    [Fact]
    public async Task CreateProjectAsync_ShouldCreateProjectWithDefaultStarterFile()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var service = new ProjectService(context);
        var userId = Guid.NewGuid();

        var request = new CreateProjectRequest(
            Name: "My Python Demo",
            Description: "A test project",
            Language: "python"
        );

        // Act
        var result = await service.CreateProjectAsync(userId, request);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("My Python Demo");
        result.Language.Should().Be("python");
        result.Files.Should().HaveCount(1);
        result.Files[0].Path.Should().Be("main.py");
        result.Files[0].IsEntryPoint.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteProjectAsync_ShouldArchiveProject()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var service = new ProjectService(context);
        var userId = Guid.NewGuid();

        var created = await service.CreateProjectAsync(userId, new CreateProjectRequest("To Delete", null, "javascript"));

        // Act
        await service.DeleteProjectAsync(created.Id, userId);

        // Assert
        var projects = await service.GetProjectsAsync(userId);
        projects.Should().BeEmpty();
    }
}
