using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Web2
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            // Set up configuration sources.
            Configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .AddJsonFile("Configuration\\appsettings.json")
                .Build();
        }

        public IConfigurationRoot Configuration { get; set; }

        // Entry point for the application.
        public static void Main(string[] args) => WebApplication.Run<Startup>(args);

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            if (env.IsDevelopment())
            {
                app.UseBrowserLink();
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseIISPlatformHandler();

            app.UseStaticFiles();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            services.AddMvc();

            var builder = new ContainerBuilder();

            //Create the directory to put all depedencies in
            var binDirectory = Environment.ExpandEnvironmentVariables(Configuration["References:PrivateBin"]);
            CreateDirectory(binDirectory);

            // Copy the core and state depedencies into the bin directory
            CopyAssemblies(Environment.ExpandEnvironmentVariables(Configuration["References:PublicBin"]), binDirectory);

            // Register the dependencies from the bin directory When all of the depedencies are in the same folder they are able to be resolved If some were in /Core and some in /State/ID they would
            // fail to resolve...
            RegisterAssemblies(binDirectory, builder);

            builder.Populate(services);
            var container = builder.Build();
            var serviceProvider = container.Resolve<IServiceProvider>();
            return serviceProvider;
        }

        private void CopyAssemblies(string source, string destination)
        {
            if (Directory.Exists(source))
                foreach (string file in Directory.GetFiles(source, "*.*", SearchOption.TopDirectoryOnly))
                {
                    var fileName = Path.GetFileName(file);
                    File.Copy(file, destination + fileName, true);
                }
        }

        private void CreateDirectory(string directory)
        {
            if (Directory.Exists(directory))
            {
                foreach (var file in Directory.GetFiles(directory))
                {
                    File.Delete(file);
                }
            }
            else
            {
                Directory.CreateDirectory(directory);
            }
        }

        /// <summary>
        /// Registers types in the supplied directory so they can be resolved from service container.
        /// </summary>
        /// <param name="directory">Directory path containing the dll's you would like to scan and register.</param>
        /// <param name="builder">ContainerBuilder that you would like to register the assemblies to.</param>
        /// <remarks>Assembly types are registered as both itself and as the interface. This will allow it to be resolved when either is needed.</remarks>
        private void RegisterAssemblies(string directory, ContainerBuilder builder)
        {
            var assemblies = Directory.GetFiles(directory, "*.dll", SearchOption.TopDirectoryOnly).Select(Assembly.LoadFrom).ToArray();

            // register singleton logger.
            //builder.RegisterAssemblyTypes(assembly).AssignableTo<ILogger>().SingleInstance().AsSelf().AsImplementedInterfaces();

            // register per request services/repositories/datacontexts.
            builder.RegisterAssemblyTypes(assemblies).Where(t =>
                t.Name.EndsWith("Service")
                || t.Name.EndsWith("Repository")
                || t.Name.EndsWith("DataContext")
            ).AsSelf().AsImplementedInterfaces();

            // register per dependency models/items.
            builder.RegisterAssemblyTypes(assemblies).Where(t =>
                !t.Name.EndsWith("Service")
                && !t.Name.EndsWith("Repository")
                && !t.Name.EndsWith("DataContext")
            ).AsSelf().AsImplementedInterfaces().InstancePerDependency();
        }
    }
}