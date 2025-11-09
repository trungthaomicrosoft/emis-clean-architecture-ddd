# Chat Worker Service - Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution file
COPY ["EMIS.sln", "./"]

# Copy all project files
COPY ["src/BuildingBlocks/EMIS.BuildingBlocks/EMIS.BuildingBlocks.csproj", "src/BuildingBlocks/EMIS.BuildingBlocks/"]
COPY ["src/BuildingBlocks/EMIS.EventBus/EMIS.EventBus.csproj", "src/BuildingBlocks/EMIS.EventBus/"]
COPY ["src/BuildingBlocks/EMIS.SharedKernel/EMIS.SharedKernel.csproj", "src/BuildingBlocks/EMIS.SharedKernel/"]
COPY ["src/Services/Chat/Chat.Domain/Chat.Domain.csproj", "src/Services/Chat/Chat.Domain/"]
COPY ["src/Services/Chat/Chat.Application/Chat.Application.csproj", "src/Services/Chat/Chat.Application/"]
COPY ["src/Services/Chat/Chat.Infrastructure/Chat.Infrastructure.csproj", "src/Services/Chat/Chat.Infrastructure/"]
COPY ["src/Services/Chat/Chat.Worker/Chat.Worker.csproj", "src/Services/Chat/Chat.Worker/"]

# Restore dependencies
RUN dotnet restore "src/Services/Chat/Chat.Worker/Chat.Worker.csproj"

# Copy all source code
COPY . .

# Build the worker project
WORKDIR "/src/src/Services/Chat/Chat.Worker"
RUN dotnet build "Chat.Worker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Chat.Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Chat.Worker.dll"]
