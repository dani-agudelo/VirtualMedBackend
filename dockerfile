# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files for restoration
COPY ["VirtualMed.Api/VirtualMed.Api.csproj", "VirtualMed.Api/"]
COPY ["VirtualMed.Application/VirtualMed.Application.csproj", "VirtualMed.Application/"]
COPY ["VirtualMed.Domain/VirtualMed.Domain.csproj", "VirtualMed.Domain/"]
COPY ["VirtualMed.Infrastructure/VirtualMed.Infrastructure.csproj", "VirtualMed.Infrastructure/"]
COPY ["VirtualMed.Tests/VirtualMed.Tests.csproj", "VirtualMed.Tests/"]

# Restore dependencies
RUN dotnet restore "VirtualMed.Api/VirtualMed.Api.csproj"

# Copy remaining source code
COPY . .

# Build and publish the API project
RUN dotnet publish "VirtualMed.Api/VirtualMed.Api.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Expose port
EXPOSE 8080
EXPOSE 443

# Copy published app from build stage
COPY --from=build /app/publish .

# Health check (optional but recommended)
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD dotnet --version || exit 1

ENTRYPOINT ["dotnet", "VirtualMed.Api.dll"]