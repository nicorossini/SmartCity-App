# SmartCity-App
Distributed application simulating a smart city
## How to run the application
In the root folder, run:
```bash
docker compose up -d
```
then run the container
```bash
docker run
```
From the terminal in your project folder:
```bash
dotnet run
```
##API Endpoints:
- `POST http://localhost:5270/api/water/sensor/temp/zone-001` - Set air temperature
- `POST http://localhost:5270/api/water/sensor/moisture/zone-001` - Set soil moisture
- `POST http://localhost:5270/api/water/zone/zone-001/simulate` - Simulate zone
- `GET http://localhost:5270/api/water/zone/zone-001/status` - Check status

