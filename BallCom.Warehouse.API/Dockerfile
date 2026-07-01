FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["BallCom.Warehouse.API/BallCom.Warehouse.API.csproj", "BallCom.Warehouse.API/"]
RUN dotnet restore "BallCom.Warehouse.API/BallCom.Warehouse.API.csproj"
COPY ["BallCom.Warehouse.API/", "BallCom.Warehouse.API/"]
RUN dotnet publish "BallCom.Warehouse.API/BallCom.Warehouse.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "BallCom.Warehouse.API.dll"]
