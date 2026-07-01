FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["BallCom.Supplier.API/BallCom.Supplier.API.csproj", "BallCom.Supplier.API/"]
RUN dotnet restore "BallCom.Supplier.API/BallCom.Supplier.API.csproj"
COPY ["BallCom.Supplier.API/", "BallCom.Supplier.API/"]
RUN dotnet publish "BallCom.Supplier.API/BallCom.Supplier.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "BallCom.Supplier.API.dll"]
