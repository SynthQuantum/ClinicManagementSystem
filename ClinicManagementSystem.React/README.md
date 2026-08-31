# ClinicManagementSystem.React

React frontend pages for CRUD operations on Patients and Staff data using the existing ClinicManagementSystem API.

## Features

- JWT login flow using `POST /api/auth/login`
- Patients CRUD page using `api/patients`
- Staff CRUD page using `api/staffmembers`
- API base URL and JWT token persisted in local storage

## Prerequisites

- Node.js 20+
- Running backend API host from this repository

## Run

1. Install dependencies:

   npm install

2. Start development server:

   npm run dev

3. Open the app URL shown by Vite (default `http://localhost:5173`).

4. Enter API base URL in the app (example: `https://localhost:7071`).

5. Login with a seeded user account to store JWT.

## Notes

- Staff endpoints are currently restricted to Admin role.
- API CORS is already permissive in Development in this repository.
- For production, configure API `AllowedCorsOrigins` explicitly.
