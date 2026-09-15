# 1. Этап сборки приложения на .NET SDK
FROM ://microsoft.com AS build
WORKDIR /src

# Копируем проект и восстанавливаем зависимости
COPY ["Ricis.WebAssembly.csproj", "./"]
RUN dotnet restore "./Ricis.WebAssembly.csproj"

# Копируем весь код и публикуем в режиме Release
COPY . .
RUN dotnet publish "Ricis.WebAssembly.csproj" -c Release -o /app/publish

# 2. Этап запуска на Nginx
FROM nginx:alpine
WORKDIR /usr/share/nginx/html

# Копируем скомпилированную статику Blazor WASM в папку Nginx
COPY --from=build /app/publish/wwwroot .

# Копируем наш конфиг Nginx
COPY nginx.conf /etc/nginx/nginx.conf

# Открываем порт для Cloud Run
EXPOSE 8080