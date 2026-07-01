FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["BallCom.Logistics.API/BallCom.Logistics.API.csproj", "BallCom.Logistics.API/"]
RUN dotnet restore "BallCom.Logistics.API/BallCom.Logistics.API.csproj"
COPY ["BallCom.Logistics.API/", "BallCom.Logistics.API/"]
RUN dotnet publish "BallCom.Logistics.API/BallCom.Logistics.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "BallCom.Logistics.API.dll"]
