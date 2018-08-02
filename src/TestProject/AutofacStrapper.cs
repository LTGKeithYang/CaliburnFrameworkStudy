using Autofac;
using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestProject
{
    class AutofacStrapper : BootstrapperBase
    {
        protected IContainer Container
        {
            get; private set;
        }

        public AutofacStrapper()
        {
            Initialize();
            var a = AssemblySource.Instance.ToArray();
            DisplayRootViewFor<ShellViewModel>();
        }

        //protected override void Configure()
        //{
        //    // configure container
        //    var builder = new ContainerBuilder();

        //    //  register view models
        //    builder.RegisterAssemblyTypes(AssemblySource.Instance.ToArray())
        //      //  must be a type that ends with ViewModel
        //      .Where(type => type.Name.EndsWith("ViewModel"))
        //      //  must implement INotifyPropertyChanged (deriving from PropertyChangedBase will statisfy this)
        //      .Where(type => type.GetInterface(typeof(System.ComponentModel.INotifyPropertyChanged).Name) != null)
        //      //  registered as self
        //      .AsSelf()
        //      .PropertiesAutowired()
        //      //  always create a new one
        //      .InstancePerDependency();

        //    //  register views
        //    builder.RegisterAssemblyTypes(AssemblySource.Instance.ToArray())
        //      //  must be a type that ends with View
        //      .Where(type => type.Name.EndsWith("View"))
        //      //  registered as self
        //      .AsSelf()
        //      //  always create a new one
        //      .InstancePerDependency();

        //    //  register the single window manager for this container
        //    builder.Register<IWindowManager>(c => new WindowManager()).InstancePerLifetimeScope();
        //    //  register the single event aggregator for this container
        //    builder.Register<IEventAggregator>(c => new EventAggregator()).InstancePerLifetimeScope();

        //    ConfigureContainer(builder);

        //    Container = builder.Build();
        //}

        private void ConfigureContainer(ContainerBuilder builder)
        {
            throw new NotImplementedException();
        }
    }
}
