using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(CGSolar.Startup))]
namespace CGSolar
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
