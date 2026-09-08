using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using OnlineCompiler.Application.Common.Interfaces;
using OnlineCompiler.Application.DTOs.Executions;
using OnlineCompiler.Application.Services;
using OnlineCompiler.Domain.Entities;
using OnlineCompiler.Infrastructure.Persistence;
using Xunit;

namespace OnlineCompiler.UnitTests;

public class ExecutionServiceTests
{
    private OnlineCompilerDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<OnlineCompilerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new OnlineCompilerDbContext(options);
    }

    [Fact]
    public async Task EnqueueExecutionAsync_ShouldCreateExecutionAndPushToQueue()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var queueMock = new Mock<IExecutionQueue>();
        var service = new ExecutionService(context, queueMock.Object);

        var userId = Guid.NewGuid();
        var project = new Project
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = "Python Execution Test",
            Language = "python"
        };
        var file = new ProjectFile
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Path = "main.py",
            Content = "print('Hello Test')",
            IsEntryPoint = true
        };

        context.Projects.Add(project);
        context.ProjectFiles.Add(file);
        await context.SaveChangesAsync();

        var request = new RunExecutionRequest(project.Id, "optional-stdin");

        // Act
        var result = await service.EnqueueExecutionAsync(userId, request);

        // Assert
        result.Should().NotBeNull();
        result.ProjectId.Should().Be(project.Id);
        result.Status.Should().Be(ExecutionStatus.Queued);
        result.Language.Should().Be("python");

        queueMock.Verify(q => q.EnqueueAsync(
            It.Is<ExecutionJobPayload>(p => p.ProjectId == project.Id && p.EntryPoint == "main.py"),
            It.IsAny<CancellationToken>()),
            Times.Once);

        var dbExecution = await context.Executions.FirstOrDefaultAsync(e => e.Id == result.Id);
        dbExecution.Should().NotBeNull();
        dbExecution!.Status.Should().Be(ExecutionStatus.Queued);
    }
}
