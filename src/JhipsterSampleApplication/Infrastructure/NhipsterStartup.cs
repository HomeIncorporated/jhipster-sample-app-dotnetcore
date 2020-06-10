using JHipsterNet.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MyCompany.Infrastructure {
    public static class NhipsterSettingsConfiguration {
        public static IServiceCollection AddNhipsterModule(this IServiceCollection @this, IConfiguration configuration)
        {
            @this.Configure<JHipsterSettings>(configuration.GetSection("jhipster"));
            return @this;
        }
    }
}
