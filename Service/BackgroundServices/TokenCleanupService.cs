using Core.Interfaces.RepositoriesInterface;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.BackgroundServices
{
    public class TokenCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public TokenCleanupService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _scopeFactory.CreateScope();
                var authRepo = scope.ServiceProvider.GetRequiredService<IAuthRepository>();

                await authRepo.CleanupExpiredRefreshTokensAsync();
                await authRepo.SaveChangesAsync();

                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }
    }


}
