# Build from repo root so TCG.Core, TCG.GameLogic, TCG.Economy are available
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and projects
COPY TCG.sln ./
COPY backend/TCG.sln backend/
COPY backend/TCG.Core/ backend/TCG.Core/
COPY backend/TCG.GameLogic/ backend/TCG.GameLogic/
COPY backend/TCG.Economy/ backend/TCG.Economy/
COPY backend/TCG.Server/ backend/TCG.Server/
COPY backend/TCG.Tests/ backend/TCG.Tests/

# Restore and publish TCG.Server (includes all dependencies)
RUN dotnet restore backend/TCG.sln
RUN dotnet publish backend/TCG.Server/TCG.Server.csproj -c Release -o /app/publish

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./out
ENTRYPOINT ["dotnet", "out/TCG.Server.dll"]
