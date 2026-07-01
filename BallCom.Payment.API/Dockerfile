FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["BallCom.Payment.API/BallCom.Payment.API.csproj", "BallCom.Payment.API/"]
RUN dotnet restore "BallCom.Payment.API/BallCom.Payment.API.csproj"
COPY ["BallCom.Payment.API/", "BallCom.Payment.API/"]
RUN dotnet publish "BallCom.Payment.API/BallCom.Payment.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "BallCom.Payment.API.dll"]
