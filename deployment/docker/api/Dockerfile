# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and csproj files for optimal layer caching
COPY ["OnlineCompiler.sln", "./"]
COPY ["src/Domain/Domain.csproj", "src/Domain/"]
COPY ["src/Application/Application.csproj", "src/Application/"]
COPY ["src/Infrastructure/Infrastructure.csproj", "src/Infrastructure/"]
COPY ["src/Web/Web.csproj", "src/Web/"]
COPY ["src/Workers/Workers.csproj", "src/Workers/"]
COPY ["tests/UnitTests/UnitTests.csproj", "tests/UnitTests/"]

RUN dotnet restore "src/Web/Web.csproj"

# Copy source code and build
COPY . .
WORKDIR "/src/src/Web"
RUN dotnet publish "Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "OnlineCompiler.Web.dll"]
