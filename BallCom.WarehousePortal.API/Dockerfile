FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["BallCom.WarehousePortal.API/BallCom.WarehousePortal.API.csproj", "BallCom.WarehousePortal.API/"]
RUN dotnet restore "BallCom.WarehousePortal.API/BallCom.WarehousePortal.API.csproj"
COPY ["BallCom.WarehousePortal.API/", "BallCom.WarehousePortal.API/"]
RUN dotnet publish "BallCom.WarehousePortal.API/BallCom.WarehousePortal.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "BallCom.WarehousePortal.API.dll"]
