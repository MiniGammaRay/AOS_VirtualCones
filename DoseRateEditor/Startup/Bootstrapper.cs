using Autofac;
using DoseRateEditor.ViewModels;
using DoseRateEditor.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using VMS.TPS.Common.Model.API;

namespace DoseRateEditor.Startup
{
    public class Bootstrapper
    {
            public IContainer Bootstrap(User user, Application app, Patient pat, Course crs, PlanSetup plan)
            {
           
                var builder = new ContainerBuilder();
                //esapi components.            
                builder.RegisterInstance<User>(user);
                builder.RegisterInstance<Application>(app);
                builder.RegisterInstance<Patient>(pat);
                builder.RegisterInstance<Course>(crs);
                builder.RegisterInstance<PlanSetup>(plan);

                //startup components.
                builder.RegisterType<MainView>().AsSelf();
                builder.RegisterType<MainViewModel>().AsSelf();

                var container = builder.Build();

                // Resolve MainViewModel and call LoadBeamTemplates
                var mainViewModel = container.Resolve<MainViewModel>();

                return container;
            }
        
    }
}
