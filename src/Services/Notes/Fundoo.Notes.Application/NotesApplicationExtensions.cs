using Fundoo.Notes.Application.Interfaces;
using Fundoo.Notes.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Fundoo.Notes.Application;

/// <summary>
/// Extension methods to register Application-layer services with the DI container.
/// </summary>
public static class NotesApplicationExtensions
{
    public static IServiceCollection AddNotesApplication(this IServiceCollection services)
    {
        services.AddScoped<INoteService, NoteService>();
        services.AddScoped<ILabelService, LabelService>();

        return services;
    }
}
