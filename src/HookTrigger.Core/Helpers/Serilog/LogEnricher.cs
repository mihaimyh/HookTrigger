﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Serilog;
using System;
using System.Linq;

namespace HookTrigger.Core.Helpers.Serilog
{
    public static class LogEnricher
    {
        /// <summary>
        /// Enriches the HTTP request log with additional data via the Diagnostic Context
        /// </summary>
        /// <param name="diagnosticContext">The Serilog diagnostic context</param>
        /// <param name="httpContext">The current HTTP Context</param>
        public static void EnrichFromRequest(IDiagnosticContext diagnosticContext, HttpContext httpContext)
        {
            diagnosticContext.Set("ClientIP", httpContext.Connection.RemoteIpAddress.ToString());
            diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].FirstOrDefault());
            diagnosticContext.Set("Resource", httpContext.GetMetricsCurrentResourceName());
        }

        public static string GetMetricsCurrentResourceName(this HttpContext httpContext)
        {
            if (httpContext == null)
                throw new ArgumentNullException(nameof(httpContext));

            var endpoint = httpContext.Features.Get<IEndpointFeature>()?.Endpoint;

#if NETCOREAPP3_1
            return endpoint?.Metadata.GetMetadata<EndpointNameMetadata>()?.EndpointName;

#else
             return endpoint?.Metadata.GetMetadata<IRouteValuesAddressMetadata>()?.RouteName;
#endif
        }
    }
}