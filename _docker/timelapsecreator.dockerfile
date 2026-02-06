FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

COPY Common/Common/Common.csproj Common/Common/
COPY Common/Common.Hosting/Common.Hosting.csproj Common/Common.Hosting/
COPY Common/Common.OpenCV/Common.OpenCV.csproj Common/Common.OpenCV/
COPY Services/TimelapseCreator/TimelapseCreator.csproj Services/TimelapseCreator/
RUN dotnet restore Services/TimelapseCreator/TimelapseCreator.csproj

COPY Common/Common/ Common/Common/
COPY Common/Common.Hosting/ Common/Common.Hosting/
COPY Common/Common.OpenCV/ Common/Common.OpenCV/
COPY Services/TimelapseCreator/ Services/TimelapseCreator/
RUN dotnet publish Services/TimelapseCreator/TimelapseCreator.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/runtime:8.0-alpine AS runtime
WORKDIR /app
COPY --from=build /app .

ENTRYPOINT ["dotnet", "TimelapseCreator.dll"]
