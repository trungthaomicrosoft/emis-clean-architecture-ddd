#!/bin/bash

# Script to add all projects to solution

cd /home/dtthao-ub/Projects/emis-clean-architecture-ddd

# Add BuildingBlocks projects
dotnet sln add src/BuildingBlocks/EMIS.SharedKernel/EMIS.SharedKernel.csproj
dotnet sln add src/BuildingBlocks/EMIS.BuildingBlocks/EMIS.BuildingBlocks.csproj
dotnet sln add src/BuildingBlocks/EMIS.EventBus/EMIS.EventBus.csproj

# Add Identity Service
dotnet sln add src/Services/Identity/Identity.API/Identity.API.csproj
dotnet sln add src/Services/Identity/Identity.Application/Identity.Application.csproj
dotnet sln add src/Services/Identity/Identity.Domain/Identity.Domain.csproj
dotnet sln add src/Services/Identity/Identity.Infrastructure/Identity.Infrastructure.csproj

# Array of service names
services=(
    "Student"
    "Teacher"
    "Attendance"
    "Assessment"
    "NewsFeed"
    "Chat"
    "Payment"
    "Menu"
    "Leave"
    "Camera"
    "Report"
    "Notification"
)

# Add all services to solution
for service in "${services[@]}"
do
    dotnet sln add "src/Services/$service/$service.API/$service.API.csproj"
    dotnet sln add "src/Services/$service/$service.Application/$service.Application.csproj"
    dotnet sln add "src/Services/$service/$service.Domain/$service.Domain.csproj"
    dotnet sln add "src/Services/$service/$service.Infrastructure/$service.Infrastructure.csproj"
done

# Add API Gateway
dotnet sln add src/ApiGateway/ApiGateway/ApiGateway.csproj

# Add Test projects
dotnet sln add tests/UnitTests/UnitTests.csproj
dotnet sln add tests/IntegrationTests/IntegrationTests.csproj

echo "All projects added to solution successfully!"
