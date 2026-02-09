# Multi-stage Dockerfile for Blazor (.NET 10)

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy everything and restore (ensure build context includes referenced projects)
COPY Common/Common/Common.csproj Common/Common/
COPY Common/Common.Hosting/Common.Hosting.csproj Common/Common.Hosting/
COPY Common/Common.OpenCV/Common.OpenCV.csproj Common/Common.OpenCV/
COPY WebApps/PortalCameras/BlazorPortalCamera.csproj WebApps/PortalCameras/BlazorPortalCamera.csproj
RUN dotnet restore "WebApps\PortalCameras\BlazorPortalCamera.csproj"

# Publish the app
COPY Common/Common Common/Common/
COPY Common/Common Common/Common.Hosting/
COPY Common/Common Common/Common.OpenCV/
COPY WebApps/PortalCameras WebApps/PortalCameras
RUN dotnet publish "WebApps/PortalCameras/BlazorPortalCamera.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS="http://+:80"
EXPOSE 80

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BlazorPortalCamera.dll"]
