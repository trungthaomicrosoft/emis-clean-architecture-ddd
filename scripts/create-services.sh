#!/bin/bash

# Script to create all microservices with Clean Architecture structure
# Each service has 4 layers: API, Application, Domain, Infrastructure

cd /home/dtthao-ub/Projects/emis-clean-architecture-ddd/src/Services

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

# Create each service with 4 layers
for service in "${services[@]}"
do
    echo "Creating $service Service..."
    
    # Create folders
    mkdir -p "$service/$service.API"
    mkdir -p "$service/$service.Application"
    mkdir -p "$service/$service.Domain"
    mkdir -p "$service/$service.Infrastructure"
    
    # Create projects
    cd "$service"
    
    dotnet new webapi -n "$service.API" --no-restore
    dotnet new classlib -n "$service.Application" --no-restore
    dotnet new classlib -n "$service.Domain" --no-restore
    dotnet new classlib -n "$service.Infrastructure" --no-restore
    
    cd ..
    
    echo "✓ $service Service created"
done

echo "All services created successfully!"
