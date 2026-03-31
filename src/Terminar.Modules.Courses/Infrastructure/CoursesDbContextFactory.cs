using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Terminar.Modules.Courses.Infrastructure;

public sealed class CoursesDbContextFactory : IDesignTimeDbContextFactory<CoursesDbContext>
{
    public CoursesDbContext CreateDbContext(string[] args)
    {
        var opts = new DbContextOptionsBuilder<CoursesDbContext>()
            .UseNpgsql("Host=localhost;Database=terminar;Username=postgres;Password=postgres")
            .Options;

        return new CoursesDbContext(opts, null!);
    }
}
