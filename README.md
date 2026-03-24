# 🌤 Weather App

Тестовое веб-приложение для отображения погодной информации.

---

##  Стек

- Backend: .NET 10 (ASP.NET Core Web API)
- Frontend: Angular 21
- База данных: PostgreSQL
- Инфраструктура: Docker Compose

---

## Запуск проекта

### 1. Запуск PostgreSQL (Docker)

Перейдите в папку `infra`
Запустите контейнер: 
docker compose --env-file .env up -d

PostgreSQL будет доступен по адресу: localhost:55432

### 2. Запуск Backend (API)
Перейдите в папку `backend`
Примените миграции (если запускаете впервые):
dotnet ef database update

Запустите API:
dotnet run

API будет доступен по адресу:
https://localhost:5000

Swagger:
https://localhost:5000/swagger

### 3. Запуск Frontend (Angular)
Перейдите в папку `frontend`
Установите зависимости:
npm install

Запустите приложение:
npm start

Откройте в браузере:
http://localhost:4200

