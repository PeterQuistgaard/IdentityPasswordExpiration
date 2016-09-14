using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(IdentityPasswordExpiration.Startup))]
namespace IdentityPasswordExpiration
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }

    }
}
