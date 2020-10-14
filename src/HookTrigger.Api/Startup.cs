using AspNetCore.ApiVersioning;
using Confluent.Kafka;
using FluentValidation.AspNetCore;
using HealthChecks.UI.Client;
using HookTrigger.Api.Services;
using HookTrigger.Core.Helpers.Serilog;
using HookTrigger.Core.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.AspNetCore.IPLogging;
using System.Net;

namespace HookTrigger.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IApiVersionDescriptionProvider provider)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseIPAddressLogging();

            app.UseHttpsRedirection();

            app.UseSerilogRequestLogging(opts => opts.EnrichDiagnosticContext = LogEnricher.EnrichFromRequest);

            app.UseSwaggerr(provider);

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/health", new HealthCheckOptions
                {
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });
                endpoints.MapHealthChecksUI();
                endpoints.MapControllers();
            });
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var config = new ProducerConfig();
            Configuration.Bind("Producer", config);
            config.ClientId = Dns.GetHostName();
            services.AddSingleton(config);

            services.AddIPAddressLogging();

            services.AddControllers()
                .AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<Startup>())
                .ConfigureApiBehaviorOptions(setupAction =>
                {
                    setupAction.InvalidModelStateResponseFactory = context =>
                    {
                        var problemDetails = new ValidationProblemDetails(context.ModelState)
                        {
                            Type = "https://wiki.codescu.com/modelvalidationproblem",
                            Title = "One or more model validation errors occurred.",
                            Status = StatusCodes.Status422UnprocessableEntity,
                            Detail = "See the errors property for details.",
                            Instance = context.HttpContext.Request.Path
                        };

                        problemDetails.Extensions.Add("traceId", context.HttpContext.TraceIdentifier);

                        return new UnprocessableEntityObjectResult(problemDetails)
                        {
                            ContentTypes = { "application/problem+json", "application/problem+xml" }
                        };
                    };
                });

            services.AddVersioning();

            services.AddScoped<IMessageSenderService<DockerHubPayload>, KafkaProducer>();

            services.AddHealthChecks()
                .AddKafka(config, tags: new string[] { "kafka" });
            //.AddSqlServer(Configuration.GetConnectionString("Default"), tags: new string[] { "sqlserver" });
            services
                .AddHealthChecksUI()
                .AddInMemoryStorage();
        }
    }
}