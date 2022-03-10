FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

COPY ./*sln ./

COPY ["OpenFTTH.UtilityGraphService.Service/OpenFTTH.UtilityGraphService.Service.csproj", "OpenFTTH.UtilityGraphService.Service/"]

RUN dotnet restore "OpenFTTH.UtilityGraphService.Service/OpenFTTH.UtilityGraphService.Service.csproj"

COPY . .
WORKDIR "/src/OpenFTTH.UtilityGraphService.Service"
RUN dotnet build "OpenFTTH.UtilityGraphService.Service.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "OpenFTTH.UtilityGraphService.Service.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OpenFTTH.UtilityGraphService.Service.dll"]
