# Stage 1: Runtime base
# Using .NET 10 ASP.NET runtime on Ubuntu (now the default for .NET 10)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Stage 2: Build
# Using .NET 10 SDK on Ubuntu
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy only the main project file to leverage Docker layer caching for restores
COPY ["PricingSystem/PricingSystem.csproj", "PricingSystem/"]
RUN dotnet restore "./PricingSystem/PricingSystem.csproj"

# Copy the rest of the source code
COPY . .
WORKDIR "/src/PricingSystem"

# Build the application
RUN dotnet build "./PricingSystem.csproj" -c %BUILD_CONFIGURATION% -o /app/build

# Stage 3: Publish
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./PricingSystem.csproj" -c %BUILD_CONFIGURATION% -o /app/publish /p:UseAppHost=false

# Stage 4: Final production image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PricingSystem.dll"]
