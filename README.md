# Centralized-Application-Management-System (CAMS)

This repository is a starter template for a modern web application using **ASP.NET Core MVC** with the classic SPA project structure, **React** for the frontend (integrated via the SPA template), and **Docker** for containerization and deployment. This setup allows for efficient development, testing, and deployment of scalable web applications.

The application is designed to act as a middleman, connecting to multiple client databases and managing user permissions centrally via service accounts or APIs.

---

## Table of Contents

- [Getting Started](#getting-started)
  - [Prerequisites](#prerequisites)
  - [Running with Docker](#running-with-docker)
  - [Running the .NET/React App without docker](#running-.NET/React-App-without-Docker)
- [Branch Naming Convention](#branch-naming-convention)
- [Contributing](#contributing)
- [License](#license)

---

## Project Structure

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Node.js (LTS)](https://nodejs.org/en/download/)
- [Docker](https://www.docker.com/products/docker-desktop/)

### Running with Docker

From the root of your project:

```sh
docker compose up
```

for a more compressed version

```sh
docker compose up -d
```

To stop:

```sh
docker compose down
```

Unforseen issues try:

```sh
docker compose down
```
remove all containers, iamges, volumes, builds(optional) then run:

```sh
docker compose up --build
```

**⚠️ WARNING:** Running `docker compose down` can potientally delete mock data!

---

## Running .NET/React App without Docker

#### Restore & Install Dependencies

```sh
dotnet restore
cd ClientApp
npm install
```

#### Run the React Development Server (optional, for hot reload)

```sh
cd ClientApp
npm start
```
This runs the React app at [http://localhost:3000](http://localhost:3000), proxying API calls to the backend.

#### Run the ASP.NET Core Backend (Without docker)

From the root project directory:

```sh
dotnet run
```
By default, this runs the ASP.Net Core MVC at [http://localhost:5266](http://localhost:8080)

---

### Run an EF Core Migration

#### Install Entity Framework tool if you don't already have
```sh
dotnet tool install --global dotnet-ef
```

#### Start Migration Process

When running this command, ensure the current directory is /Prototype

```sh
dotnet ef migrations add MigrationMessage
```

When this command is done, you should see something like this
```sh
Build started...
Build succeeded.
Done. To undo this action, use 'ef migrations remove'
```

#### Upload Migrated Tables to Sql Server

Run this follow up script to populate the sql server
```sh
dotnet ef database update
```
If done correctly, there should be no errors and the last statment should be
```sh
Done.
```

If you run into a bug, running those commands this should hopefully fix it.
```sh
rm -rf bin/
rm -rf obj/
dotnet restore
dotnet clean
dotnet build
```

---

## Branch Naming Convention

When creating a new branch, please follow this naming convention for clarity and consistency:

```
<type>/<short-description>
```

- **type**: The type of work being done. Examples:
  - `feature` — for new features
  - `bugfix` — for bug fixes
  - `hotfix` — for urgent fixes
  - `docs` — for documentation changes
  - `chore` — for maintenance tasks

- **short-description**: Brief description of the branch purpose, using hyphens (`-`) to separate words.

**Examples:**
- `feature/user-authentication`
- `bugfix/login-error`
- `docs/update-readme`
- `chore/cleanup-docker-files`

---

## Contributing

1. Fork the repository.
2. Create your branch (`git checkout -b feature/your-feature`).
3. Commit your changes.
4. Push to the branch (`git push origin feature/your-feature`).
5. Create a Pull Request.

---

## License

This project is licensed under the [MIT License](LICENSE).
