FROM mcr.microsoft.com/dotnet/sdk:10.0 AS builder
WORKDIR src
COPY ./VSpark.csproj .
RUN dotnet restore ./VSpark.csproj
COPY . .
RUN dotnet publish -c Debug -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR app
COPY --from=builder /app/publish .

ENTRYPOINT ["dotnet", "VSpark.dll"]