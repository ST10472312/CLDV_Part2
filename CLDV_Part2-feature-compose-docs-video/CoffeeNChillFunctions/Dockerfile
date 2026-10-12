FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY ["CoffeeNChillFunctions.csproj", "./"]

RUN dotnet restore "CoffeeNChillFunctions.csproj"

COPY . .

RUN dotnet publish "CoffeeNChillFunctions.csproj" \
    -c Release \
    -o /app/publish


FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated10.0

WORKDIR /home/site/wwwroot

COPY --from=build /app/publish .

ENV AzureFunctionsJobHost__Logging__Console__IsEnabled=true