var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.PricingSystem>("pricingsystem");

builder.Build().Run();
