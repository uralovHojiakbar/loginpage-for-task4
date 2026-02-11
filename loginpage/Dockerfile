# ========= build stage =========
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

# ========= runtime stage =========
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .

# Render odatda PORT=10000 beradi. Biz Program.cs'da PORT'ni o'qiymiz.
EXPOSE 10000

ENTRYPOINT ["dotnet", "loginpage.dll"]
