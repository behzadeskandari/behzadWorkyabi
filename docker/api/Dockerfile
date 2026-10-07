FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY IranJob.sln ./
COPY src/BuildingBlocks/IranJob.SharedKernel/IranJob.SharedKernel.csproj src/BuildingBlocks/IranJob.SharedKernel/
COPY src/BuildingBlocks/IranJob.BuildingBlocks.Infrastructure/IranJob.BuildingBlocks.Infrastructure.csproj src/BuildingBlocks/IranJob.BuildingBlocks.Infrastructure/
COPY src/Host/IranJob.Api/IranJob.Api.csproj src/Host/IranJob.Api/

RUN dotnet restore src/Host/IranJob.Api/IranJob.Api.csproj

COPY src/ src/
COPY IranJob.Modules.EmployerProfile.Application/ IranJob.Modules.EmployerProfile.Application/
COPY IranJob.Modules.EmployerProfile.Domain/ IranJob.Modules.EmployerProfile.Domain/
COPY IranJob.Modules.EmployerProfile.Infrastructure/ IranJob.Modules.EmployerProfile.Infrastructure/
COPY IranJob.Modules.EmployerProfile.Presentation/ IranJob.Modules.EmployerProfile.Presentation/
RUN dotnet publish src/Host/IranJob.Api/IranJob.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
RUN mkdir -p /app/uploads
EXPOSE 8080
ENTRYPOINT ["dotnet", "IranJob.Api.dll"]
