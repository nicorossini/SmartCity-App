# Smart City Traffic Service

A real-time traffic monitoring system built with .NET 8, Orleans, and various infrastructure components for managing traffic sensors and congestion detection in smart cities.

## Features

- **Real-time Traffic Monitoring**: Monitor vehicle counts across multiple sensors
- **Congestion Detection**: Automatic alerts when traffic exceeds thresholds 
- **Historical Data Storage**: Persistent storage of traffic patterns using MongoDB
- **Caching Layer**: Redis integration for fast data access
- **Event-Driven Architecture**: RabbitMQ for real-time event publishing
- **Auto-scaling**: Orleans grain-based architecture for horizontal scaling
- **REST API**: Comprehensive API for traffic data management

## Architecture

The system uses the Orleans Actor Model with the following key components:

- **TrafficSensorGrain**: Individual sensors that simulate and track traffic data
- **TrafficManagerGrain**: Central coordinator for all sensors and congestion analysis
- **TrafficController**: HTTP API endpoints for external integration
- **Infrastructure Services**: MongoDB, Redis, and RabbitMQ for data persistence and messaging

The service is built using:

- **ASP.NET Core 8.0** - Web API framework
- **Microsoft Orleans** - Virtual actor model for distributed computing
- **MongoDB** - Document database for historical data
- **Redis** - In-memory cache for current traffic data
- **RabbitMQ** - Message broker for event streaming
- **Swagger/OpenAPI** - API documentation

## Prerequisites

Before running the application, ensure you have the following installed:

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [MongoDB](https://www.mongodb.com/try/download/community) (v6.0+)
- [Redis](https://redis.io/download) (v6.0+)
- [RabbitMQ](https://www.rabbitmq.com/download.html) (v3.8+)

## Installation & Setup

### 1. Clone the Repository
```bash
git clone https://github.com/nicorossini/SmartCity-App/
cd Traffic-Service
```

### 2. Start Infrastructure Services

Start the required infrastructure services using Docker Compose:

```bash
docker-compose up -d
```

This will start:
- **MongoDB** on port `27017`
- **Redis** on port `6379`
- **Redis Insight** (Web UI) on port `8001`
- **RabbitMQ** on port `5672` (Management UI on `15672`)

### 3. Verify Infrastructure Services

- **MongoDB**: Ready when container shows "Waiting for connections"
- **Redis**: Access Redis Insight at http://localhost:8001
- **RabbitMQ Management**: Access at http://localhost:15672 (guest/guest)

### 4. Run the Application

Open the project on Visual Studio and launch **https** command.

The service will start on:
- Swagger UI: `https://localhost:7176/swagger/index.html`

The application will:
- Start on `https://localhost:7176` or `http://localhost:5156`
- Initialize Orleans cluster
- Automatically register 5 test sensors
- Begin simulating traffic data


Alternatively:
```bash
dotnet restore
dotnet run
```

## API Endpoints

### Sensor Management
- `POST /api/traffic/sensors/{sensorId}/register` - Register a new traffic sensor
- `GET /api/traffic/sensors/{sensorId}/data` - Get current sensor data
- `GET /api/traffic/sensors/all` - Get all sensor data

### Traffic Monitoring
- `GET /api/traffic/congestion/top?count=5` - Get most congested areas
- `GET /api/traffic/sensors/{sensorId}/congestion` - Check if area is congested
- `POST /api/traffic/sensors/{sensorId}/simulate` - Simulate traffic data

### Health Check
- `GET /health` - Service health status

## Usage Examples

### Register a Traffic Sensor
```bash
curl -X POST "https://localhost:7176/api/traffic/sensors/SENSOR_006/register" \
  -H "Content-Type: application/json" \
  -d '{"Location": "Main Street - Downtown"}'
```

### Simulate Traffic Data
```bash
curl -X POST "https://localhost:7176/api/traffic/sensors/SENSOR_001/simulate" \
  -H "Content-Type: application/json" \
  -d '{"VehicleCount": 95}'
```

### Get Congested Areas
```bash
curl -X GET "https://localhost:7176/api/traffic/congestion/top?count=3"
```

## Traffic Simulation

The service automatically simulates realistic traffic patterns based on time of day:

- **Morning Rush (6-9 AM)**: 80-120 vehicles
- **Mid-day (10 AM-4 PM)**: 30-60 vehicles  
- **Evening Rush (5-7 PM)**: 90-130 vehicles
- **Evening (8-11 PM)**: 20-40 vehicles
- **Night/Early Morning**: 5-15 vehicles

Congestion alerts are triggered when vehicle count exceeds **80 vehicles** per monitoring period.

## Test Data

The application automatically initializes 5 test sensors on startup:

- SENSOR_001: Via Roma - Piazza Centrale
- SENSOR_002: Corso Italia - Via Garibaldi  
- SENSOR_003: Viale Europa - Via Nazionale
- SENSOR_004: Piazza Duomo - Via Mazzini
- SENSOR_005: Stazione Centrale - Via Veneto

## Monitoring & Observability

### Orleans Dashboard
Access the Orleans Dashboard at: `http://localhost:8080`

### Logs
The application uses structured logging. View logs in the console or configure external log providers.

### RabbitMQ Management
Access RabbitMQ management interface at: `http://localhost:15672`
- Username: `guest`
- Password: `guest`

## Data Storage

### MongoDB Collections
- `traffic_records`: Historical traffic data with timestamps
- Schema includes: SensorId, Location, VehicleCount, Timestamp, IsCongested

### Redis Keys
- `traffic_current_data`: Current traffic data across all sensors
- `traffic_sensor:{sensorId}`: Individual sensor data cache

## Development

### Project Structure

```
Traffic_Service/
├── Controllers/          # HTTP API controllers
├── Grains/               # Orleans grain implementations
├── Interfaces/           # Grain interfaces and data models
├── Services/             # Infrastructure service implementations
├── Program.cs            # Application startup
└── docker-compose.yml    # Infrastructure services
```

### Adding New Sensors

Sensors are automatically activated when registered and begin periodic updates every 2 minutes with realistic traffic simulation.

### Key Components
- **TrafficController**: REST API endpoints
- **TrafficManagerGrain**: Central traffic coordination
- **TrafficSensorGrain**: Individual sensor management
- **MongoDbService**: Historical data persistence
- **RedisService**: Caching layer
- **RabbitMQService**: Event publishing

## Troubleshooting

### Common Issues

1. **Orleans Initialization Failed**
   - Ensure no other Orleans applications are running on the same ports
   - Check application logs for detailed error messages

2. **Infrastructure Connection Issues**
   - Verify all Docker containers are running: `docker-compose ps`
   - Check container logs: `docker-compose logs [service-name]`

3. **Test Sensors Not Initializing**
   - The application retries sensor initialization 5 times
   - Check Orleans cluster status in application logs

### Stopping Services

```bash
# Stop the .NET application
Ctrl+C

# Stop and remove infrastructure containers
docker-compose down

# Stop and remove containers with volumes
docker-compose down -v
```

## Support

For questions or issues, please create an issue in the repository or contact the development team.
