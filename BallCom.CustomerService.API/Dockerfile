FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["BallCom.CustomerService.API/BallCom.CustomerService.API.csproj", "BallCom.CustomerService.API/"]
RUN dotnet restore "BallCom.CustomerService.API/BallCom.CustomerService.API.csproj"
COPY ["BallCom.CustomerService.API/", "BallCom.CustomerService.API/"]
RUN dotnet publish "BallCom.CustomerService.API/BallCom.CustomerService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "BallCom.CustomerService.API.dll"]
