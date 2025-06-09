# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# Depending on the operating system of the host machines(s) that will build or run the containers, the image specified in the FROM statement may need to be changed.
# For more information, please see https://aka.ms/containercompat

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:9.0-nanoserver-1809 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:9.0-nanoserver-1809 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["samples/SWA/Directory.Build.props", "samples/SWA/"]
COPY ["samples/Directory.Build.props", "samples/"]
COPY ["Directory.Build.props", "."]
COPY ["Directory.Build.targets", "."]
COPY ["samples/SWA/SWACore/SWACore.csproj", "samples/SWA/SWACore/"]
COPY ["samples/SWA/SWALibrary/SWALibrary.csproj", "samples/SWA/SWALibrary/"]
RUN dotnet restore "./samples/SWA/SWACore/SWACore.csproj"
COPY . .
WORKDIR "/src/samples/SWA/SWACore"
RUN dotnet build "./SWACore.csproj" -c %BUILD_CONFIGURATION% -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./SWACore.csproj" -c %BUILD_CONFIGURATION% -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SWACore.dll"]