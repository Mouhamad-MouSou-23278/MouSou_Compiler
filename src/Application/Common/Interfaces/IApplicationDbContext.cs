using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OnlineCompiler.Domain.Entities;

namespace OnlineCompiler.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Project> Projects { get; }
    DbSet<ProjectFile> ProjectFiles { get; }
    DbSet<Execution> Executions { get; }
    DbSet<AIJob> AIJobs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
