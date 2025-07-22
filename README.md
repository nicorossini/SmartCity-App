# SmartCity-App

A distributed application simulating a smart city through interconnected microservices.

## Overview

This repository contains three distinct applications that simulate different aspects of a smart city:

- **Water Distribution Service** - Water distribution management
- **Parking Service** - Parking management
- **Traffic Service** - Traffic management

## Architecture

The application uses a microservices architecture with the following technological components:

- **MongoDB** - Database for data persistence
- **RabbitMQ** - Message broker for asynchronous communication between services
- **Redis** - In-memory cache to improve performance
- **Docker** - Service containerization

## Requirements

- Visual Studio
- Docker and Docker Compose
- .NET Framework/Core

## Installation and Setup

### 1. Clone the Repository

```bash
git clone (https://github.com/nicorossini/SmartCity-App
cd SmartCity-App
```

### 2. Start Docker Containers

Execute the Docker Compose command for each service:

```bash
# For each service
docker-compose up -d
```

### 3. Visual Studio Configuration

1. Open each application in Visual Studio as a Solution
2. Right-click on the Solution
3. Select **"Set StartUp Projects"**
4. Choose **"Multiple startup projects"**
5. Set all projects to **"Start"**
6. Click **"Apply"** → **"OK"**

### 4. Execution

After completing the configuration, all applications will start simultaneously and perform the following operations:

- Creating and writing to MongoDB databases
- Communication through RabbitMQ
- Interaction with Redis for caching
