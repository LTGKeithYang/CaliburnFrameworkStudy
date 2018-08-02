namespace Caliburn.Micro {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Windows;
    using System.Windows.Threading;

    /// <summary>
    /// Inherit from this class in order to customize the configuration of the framework.
    /// 继承自该类型可对框架进行自定义配置。
    /// </summary>
    public abstract class BootstrapperBase {
        readonly bool useApplication;
        bool isInitialized;

        /// <summary>
        /// The application.
        /// Application的引用
        /// </summary>
        protected Application Application { get; set; }

        /// <summary>
        /// Creates an instance of the bootstrapper.
        /// 构造函数
        /// </summary>
        /// <param name="useApplication">Set this to false when hosting Caliburn.Micro inside and Office or WinForms application. The default is true.</param>
        protected BootstrapperBase(bool useApplication = true) {
            this.useApplication = useApplication;
        }

        #region 初始化时调用,包含三个方法
        /// <summary>
        /// Initialize the framework.
        /// 初始化该框架。
        /// </summary>
        public void Initialize() {
            if(isInitialized) {
                return;
            }

            isInitialized = true;

            PlatformProvider.Current = new XamlPlatformProvider();

#if WP8 || NET45
            var baseExtractTypes = AssemblySourceCache.ExtractTypes;

            AssemblySourceCache.ExtractTypes = assembly =>
            {
                var baseTypes = baseExtractTypes(assembly);
                var elementTypes = assembly.GetExportedTypes()
                    .Where(t => typeof(UIElement).IsAssignableFrom(t));

                return baseTypes.Union(elementTypes);
            };

            AssemblySource.Instance.Refresh();
#endif

            if(Execute.InDesignMode) {
                try {
                    StartDesignTime();
                }catch {
                    //if something fails at design-time, there's really nothing we can do...
                    isInitialized = false;
                    throw;
                }
            }
            else {
                StartRuntime();
            }
        }

        /// <summary>
        /// Called by the bootstrapper's constructor at design time to start the framework.
        /// 设计模式下由Initialize函数调用它
        /// </summary>
        protected virtual void StartDesignTime() {
            AssemblySource.Instance.Clear();
            AssemblySource.Instance.AddRange(SelectAssemblies());

            Configure();
            IoC.GetInstance = GetInstance;
            IoC.GetAllInstances = GetAllInstances;
            IoC.BuildUp = BuildUp;
        }

        /// <summary>
        /// Called by the bootstrapper's constructor at runtime to start the framework.
        /// 运行模式下由Initialize函数调用它
        /// </summary>
        protected virtual void StartRuntime() {
            EventAggregator.HandlerResultProcessing = (target, result) => {
                var task = result as System.Threading.Tasks.Task;
                if (task != null) {
                    result = new IResult[] {task.AsResult()};
                }

                var coroutine = result as IEnumerable<IResult>;
                if (coroutine != null) {
                    var viewAware = target as IViewAware;
                    var view = viewAware != null ? viewAware.GetView() : null;
                    var context = new CoroutineExecutionContext { Target = target, View = view };

                    Coroutine.BeginExecute(coroutine.GetEnumerator(), context);
                }
            };

            AssemblySourceCache.Install();
            AssemblySource.Instance.AddRange(SelectAssemblies());

            if (useApplication) {
                Application = Application.Current;
                PrepareApplication();
            }

            Configure();
            IoC.GetInstance = GetInstance;
            IoC.GetAllInstances = GetAllInstances;
            IoC.BuildUp = BuildUp;
        }

        /// <summary>
        /// Provides an opportunity to hook into the application object.
        /// 给Application的启动，退出，未捕捉的异常处理(只能处理UI线程上的)
        /// </summary>
        protected virtual void PrepareApplication() {
            Application.Startup += OnStartup;
#if SILVERLIGHT
            Application.UnhandledException += OnUnhandledException;
#else
            Application.DispatcherUnhandledException += OnUnhandledException;
#endif
            Application.Exit += OnExit;
        }
        #endregion

        #region 8个需要重写的虚方法

        #region 五个IOC配置相关方法
        ///在<see cref="StartDesignTime" /> 或者<see cref= "StartRuntime" /> 中调用

        /// <summary>
        /// Override to configure the framework and setup your IoC container.
        /// 重写框架配置病并设置IOC容器
        /// </summary>
        protected virtual void Configure() { }

        /// <summary>
        /// Override to tell the framework where to find assemblies to inspect for views, etc.
        /// 通过重写告诉框架去哪些程序集中去检索窗口
        /// </summary>
        /// <returns>A list of assemblies to inspect.</returns>
        protected virtual IEnumerable<Assembly> SelectAssemblies() {
            return new[] { GetType().Assembly };
        }

        /// <summary>
        /// Override this to provide an IoC specific implementation.
        /// 通过提供的IOC具体接口重写
        /// </summary>
        /// <param name="service">The service to locate.</param>
        /// <param name="key">The key to locate.</param>
        /// <returns>The located service.</returns>
        protected virtual object GetInstance(Type service, string key) {
#if NET
            if (service == typeof(IWindowManager))
                service = typeof(WindowManager);
#endif

            return Activator.CreateInstance(service);
        }

        /// <summary>
        /// Override this to provide an IoC specific implementation
        /// 通过提供的IOC具体接口重写
        /// </summary>
        /// <param name="service">The service to locate.</param>
        /// <returns>The located services.</returns>
        protected virtual IEnumerable<object> GetAllInstances(Type service) {
            return new[] { Activator.CreateInstance(service) };
        }

        /// <summary>
        /// Override this to provide an IoC specific implementation.
        /// 通过提供的IOC具体接口重写
        /// </summary>
        /// <param name="instance">The instance to perform injection on.</param>
        protected virtual void BuildUp(object instance) { }
        #endregion

        #region  PrepareApplication中调用的三个与PrepareApplication相关的方法
        ///<see cref="PrepareApplication" />

        /// <summary>
        /// Override this to add custom behavior to execute after the application starts.
        /// 重写自定义行为在应用程序开始时调用
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The args.</param>
        protected virtual void OnStartup(object sender, StartupEventArgs e) { }

        /// <summary>
        /// Override this to add custom behavior on exit.
        /// 重写自定义行为在退出时调用
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event args.</param>
        protected virtual void OnExit(object sender, EventArgs e) { }

#if SILVERLIGHT
        /// <summary>
        /// Override this to add custom behavior for unhandled exceptions.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event args.</param>
        protected virtual void OnUnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e) { }
#else
        /// <summary>
        /// Override this to add custom behavior for unhandled exceptions.
        /// 重写自定义行为给UI线程中未处理的异常
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event args.</param>
        protected virtual void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e) { }
#endif
        #endregion

        #endregion

        #region 设置根启动界面
#if SILVERLIGHT && !WINDOWS_PHONE
        /// <summary>
        /// Locates the view model, locates the associate view, binds them and shows it as the root view.
        /// </summary>
        /// <param name="viewModelType">The view model type.</param>
        protected void DisplayRootViewFor(Type viewModelType) {
            var viewModel = IoC.GetInstance(viewModelType, null);
            var view = ViewLocator.LocateForModel(viewModel, null, null);

            ViewModelBinder.Bind(viewModel, view, null);

            var activator = viewModel as IActivate;
            if(activator != null)
                activator.Activate();

            Mouse.Initialize(view);
            Application.RootVisual = view;
        }

        /// <summary>
        /// Locates the view model, locates the associate view, binds them and shows it as the root view.
        /// </summary>
        /// <typeparam name="TViewModel">The view model type.</typeparam>
        protected void DisplayRootViewFor<TViewModel>() {
            DisplayRootViewFor(typeof(TViewModel));
        }
#elif NET
        /// <summary>
        /// Locates the view model, locates the associate view, binds them and shows it as the root view.
        /// 通过viewmodel寻找到关联的view，将它们绑定并作为根视图呈现。
        /// </summary>
        /// <param name="viewModelType">The view model type.</param>
        /// <param name="settings">The optional window settings.</param>
        protected void DisplayRootViewFor(Type viewModelType, IDictionary<string, object> settings = null) {
            var windowManager = IoC.Get<IWindowManager>();
            windowManager.ShowWindow(IoC.GetInstance(viewModelType, null), null, settings);
        }

        /// <summary>
        /// Locates the view model, locates the associate view, binds them and shows it as the root view.
        /// /// 通过viewmodel寻找到关联的view，将它们绑定并作为根视图呈现。
        /// </summary>
        /// <typeparam name="TViewModel">The view model type.</typeparam>
        /// <param name="settings">The optional window settings.</param>
        protected void DisplayRootViewFor<TViewModel>(IDictionary<string, object> settings = null) {
            DisplayRootViewFor(typeof(TViewModel), settings);
        }
#endif
        #endregion
    }
}
