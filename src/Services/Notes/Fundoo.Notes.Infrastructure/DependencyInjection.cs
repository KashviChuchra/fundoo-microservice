using Fundoo.Notes.Application.Interfaces;
using Fundoo.Notes.Infrastructure.Persistence;
using Fundoo.Notes.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fundoo.Notes.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddNotesInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<NotesDbConnectionFactory>();

            services.AddScoped<INoteRepository, NoteRepository>();
            services.AddScoped<ILabelRepository, LabelRepository>();

            return services;
        }
    }
}