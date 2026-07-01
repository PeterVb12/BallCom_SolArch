FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["BallCom.CustomerServicePortal.API/BallCom.CustomerServicePortal.API.csproj", "BallCom.CustomerServicePortal.API/"]
RUN dotnet restore "BallCom.CustomerServicePortal.API/BallCom.CustomerServicePortal.API.csproj"
COPY ["BallCom.CustomerServicePortal.API/", "BallCom.CustomerServicePortal.API/"]
RUN dotnet publish "BallCom.CustomerServicePortal.API/BallCom.CustomerServicePortal.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "BallCom.CustomerServicePortal.API.dll"]
