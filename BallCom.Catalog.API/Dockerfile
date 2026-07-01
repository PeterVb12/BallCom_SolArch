FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["BallCom.Catalog.API/BallCom.Catalog.API.csproj", "BallCom.Catalog.API/"]
RUN dotnet restore "BallCom.Catalog.API/BallCom.Catalog.API.csproj"
COPY ["BallCom.Catalog.API/", "BallCom.Catalog.API/"]
RUN dotnet publish "BallCom.Catalog.API/BallCom.Catalog.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "BallCom.Catalog.API.dll"]
