using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Terminar.Modules.Courses.Application.Ports;
using Terminar.Modules.Courses.Domain.Repositories;
using Terminar.Modules.Courses.Infrastructure.Ports;
using Terminar.Modules.Courses.Infrastructure.Repositories;

namespace Terminar.Modules.Courses.Infrastructure;

public static class CoursesModule
{
    public static IServiceCollection AddCoursesModule(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<CoursesDbContext>(opts =>
            opts.UseNpgsql(connectionString));

        services.AddScoped<ICourseRepository, CourseRepository>();
        services.AddScoped<ICourseCapacityReader, CourseCapacityReader>();

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(CoursesModule).Assembly));

        services.AddValidatorsFromAssembly(typeof(CoursesModule).Assembly);

        return services;
    }
}
