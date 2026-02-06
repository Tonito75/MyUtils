FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

COPY Common/Common/Common.csproj Common/Common/
COPY Apis/ApiFreeBoxCore/Domain/Domain.csproj Apis/ApiFreeBoxCore/Domain/
COPY Apis/ApiFreeBoxCore/Infrastructure/Infrastructure.csproj Apis/ApiFreeBoxCore/Infrastructure/
COPY Apis/ApiFreeBoxCore/Application/Application.csproj Apis/ApiFreeBoxCore/Application/
COPY Apis/ApiFreeBoxCore/EndPoints/EndPoints.csproj Apis/ApiFreeBoxCore/EndPoints/
RUN dotnet restore Apis/ApiFreeBoxCore/EndPoints/EndPoints.csproj

COPY Common/Common/ Common/Common/
COPY Apis/ApiFreeBoxCore/ Apis/ApiFreeBoxCore/
RUN dotnet publish Apis/ApiFreeBoxCore/EndPoints/EndPoints.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS runtime
WORKDIR /app
COPY --from=build /app .

EXPOSE 8080
ENTRYPOINT ["dotnet", "EndPoints.dll"]
