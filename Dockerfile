# Use the official .NET 9.0 SDK image as a build image
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /avancira-backend

# Copy the .csproj and restore as distinct layers
COPY . .

WORKDIR /avancira-backend/api/Avancira.API
RUN dotnet restore

RUN dotnet publish -c Release -o out

# Use a .NET 9.0 runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0
COPY --from=build /avancira-backend/api/Avancira.API/out .

# Start the application
ENTRYPOINT ["dotnet", "Avancira.API.dll"]