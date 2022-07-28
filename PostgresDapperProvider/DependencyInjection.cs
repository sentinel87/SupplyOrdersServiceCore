using Microsoft.Extensions.DependencyInjection;
using SupplyOrdersServiceCore.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PostgresDapperProvider
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddDapperDatabaseProvider(this IServiceCollection services)
        {
            services.AddSingleton<IDatabaseService, DapperDatabaseService>();
            return services;
        }
    }
}
