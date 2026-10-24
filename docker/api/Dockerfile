FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY IranJob.sln ./
COPY src/ src/
COPY IranJob.Modules.EmployerProfile.Application/ IranJob.Modules.EmployerProfile.Application/
COPY IranJob.Modules.EmployerProfile.Domain/ IranJob.Modules.EmployerProfile.Domain/
COPY IranJob.Modules.EmployerProfile.Infrastructure/ IranJob.Modules.EmployerProfile.Infrastructure/
COPY IranJob.Modules.EmployerProfile.Presentation/ IranJob.Modules.EmployerProfile.Presentation/

RUN dotnet restore src/Host/IranJob.Api/IranJob.Api.csproj
RUN dotnet publish src/Host/IranJob.Api/IranJob.Api.csproj -c Release --no-restore -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
RUN mkdir -p /app/uploads /app/cv-storage
EXPOSE 8080
ENTRYPOINT ["dotnet", "IranJob.Api.dll"]
