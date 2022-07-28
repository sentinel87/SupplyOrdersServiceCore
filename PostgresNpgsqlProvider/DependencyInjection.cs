using Microsoft.Extensions.DependencyInjection;
using SupplyOrdersServiceCore.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PostgresNpgsqlProvider
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddNpgsqlDatabaseProvider(this IServiceCollection services)
        {
            services.AddSingleton<IDatabaseService, PostgresDatabaseService>();
            return services;
        }
    }
}
