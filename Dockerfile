FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
ENV SolutionDir /src
WORKDIR /src
COPY . .
# Generate model files
WORKDIR "/src/Edge"
RUN dotnet build "Edge.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Edge.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Edge.dll"]
