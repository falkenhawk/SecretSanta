FROM mcr.microsoft.com/dotnet/core/sdk:2.1 AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY ./SecretSanta/*.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY ./SecretSanta/. ./
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/core/runtime:2.1
WORKDIR /App_Data
WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "SecretSanta.dll"]