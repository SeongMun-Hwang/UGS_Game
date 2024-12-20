// This file was generated by Cloud Code Bindings Generator. Modifications will be lost upon regeneration.
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace Unity.Services.CloudCode.GeneratedBindings
{
    public class ProjectBindings
    {
        readonly ICloudCodeService k_Service;
        public ProjectBindings(ICloudCodeService service)
        {
            k_Service = service;
        }

        public async Task<string> SayHello(string name)
        {
            return await k_Service.CallModuleEndpointAsync<string>(
                "Project",
                "SayHello",
                new Dictionary<string, object>()
                {
                    {"name", name},
                });
        }
    }
}