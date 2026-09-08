# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["OnlineCompiler.sln", "./"]
COPY ["src/Domain/Domain.csproj", "src/Domain/"]
COPY ["src/Application/Application.csproj", "src/Application/"]
COPY ["src/Infrastructure/Infrastructure.csproj", "src/Infrastructure/"]
COPY ["src/Web/Web.csproj", "src/Web/"]
COPY ["src/Workers/Workers.csproj", "src/Workers/"]
COPY ["tests/UnitTests/UnitTests.csproj", "tests/UnitTests/"]

RUN dotnet restore "src/Workers/Workers.csproj"

COPY . .
WORKDIR "/src/src/Workers"
RUN dotnet publish "Workers.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime Stage
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime
WORKDIR /app
ENV DOTNET_ENVIRONMENT=Production

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "OnlineCompiler.Workers.dll"]
