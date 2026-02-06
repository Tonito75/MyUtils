FROM mcr.microsoft.com/dotnet/sdk:10.0-preview-alpine AS build
WORKDIR /src

COPY Apis/ApiMeteo/ApiMeteo.csproj Apis/ApiMeteo/
RUN dotnet restore Apis/ApiMeteo/ApiMeteo.csproj

COPY Apis/ApiMeteo/ Apis/ApiMeteo/
RUN dotnet publish Apis/ApiMeteo/ApiMeteo.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview-alpine AS runtime
WORKDIR /app
COPY --from=build /app .

EXPOSE 8080
ENTRYPOINT ["dotnet", "ApiMeteo.dll"]
