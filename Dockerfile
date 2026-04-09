FROM node:22-alpine AS frontend-build
WORKDIR /src/frontend
COPY ["frontend coffee demo/package.json", "frontend coffee demo/package-lock.json", "./"]
RUN npm ci
COPY ["frontend coffee demo/", "./"]
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS backend-build
WORKDIR /src
COPY ["NguyenDacQuan_2123110483/NguyenDacQuan_2123110483.csproj", "NguyenDacQuan_2123110483/"]
RUN dotnet restore "NguyenDacQuan_2123110483/NguyenDacQuan_2123110483.csproj"
COPY . .
RUN dotnet publish "NguyenDacQuan_2123110483/NguyenDacQuan_2123110483.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=backend-build /app/publish .
COPY --from=frontend-build /src/frontend/dist ./wwwroot
ENTRYPOINT ["dotnet", "NguyenDacQuan_2123110483.dll"]
