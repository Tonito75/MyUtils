FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

COPY Common/Common/Common.csproj Common/Common/
COPY Common/Common.Hosting/Common.Hosting.csproj Common/Common.Hosting/
COPY Common/Common.OpenCV/Common.OpenCV.csproj Common/Common.OpenCV/
COPY Services/CameraWatcher/CameraWatcher.csproj Services/CameraWatcher/
RUN dotnet restore Services/CameraWatcher/CameraWatcher.csproj

COPY Common/Common/ Common/Common/
COPY Common/Common.Hosting/ Common/Common.Hosting/
COPY Common/Common.OpenCV/ Common/Common.OpenCV/
COPY Services/CameraWatcher/ Services/CameraWatcher/
RUN dotnet publish Services/CameraWatcher/CameraWatcher.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/runtime:8.0-alpine AS runtime
WORKDIR /app
COPY --from=build /app .

ENTRYPOINT ["dotnet", "CameraWatcher.dll"]
