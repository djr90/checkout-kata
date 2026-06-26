var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.CheckoutKata_Api>("checkout-api");

builder.Build().Run();
