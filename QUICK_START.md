# 🚀 Quick Start Guide - EMIS

## 📋 Prerequisites

### Required Software
- ✅ **.NET 8.0 SDK** or later: [Download](https://dotnet.microsoft.com/download)
- ✅ **Docker Desktop**: [Download](https://www.docker.com/products/docker-desktop)
- ✅ **Git**: [Download](https://git-scm.com/)

### Recommended Tools
- **Visual Studio 2022** (Windows) OR
- **Visual Studio Code** with C# Dev Kit (Cross-platform) OR
- **JetBrains Rider** (Cross-platform)

### Database Tools (Optional)
- **MySQL Workbench** for MySQL
- **MongoDB Compass** for MongoDB
- **Redis Insight** for Redis

---

## 🏃 Quick Start (5 minutes)

### 1. Clone Repository
```bash
git clone https://github.com/your-org/emis-clean-architecture-ddd.git
cd emis-clean-architecture-ddd
```

### 2. Start Infrastructure Services
```bash
# Start all infrastructure (MySQL, MongoDB, Redis, RabbitMQ, MinIO, etc.)
docker-compose up -d

# Verify all services are running
docker-compose ps

# Expected output: All services should show "Up" status
```

**Services URLs:**
- 🗄️ **MySQL:** localhost:3306
- 🍃 **MongoDB:** localhost:27017
- 🔴 **Redis:** localhost:6379
- 🐰 **RabbitMQ:** localhost:5672 (Management UI: http://localhost:15672)
- 📦 **MinIO:** http://localhost:9000 (Console: http://localhost:9001)
- 📊 **Elasticsearch:** http://localhost:9200
- 📈 **Kibana:** http://localhost:5601

**Default Credentials (for all services):**
- Username: `admin`
- Password: `EMISPassword123!`

### 3. Build Solution
```bash
# Restore all NuGet packages
dotnet restore

# Build entire solution
dotnet build

# Expected output: Build succeeded. 0 Warning(s). 0 Error(s).
```

### 4. Run Your First Service
```bash
# Navigate to Identity Service
cd src/Services/Identity/Identity.API

# Run the service
dotnet run

# Service will start on: https://localhost:5001
```

Open your browser and navigate to:
- **Swagger UI:** https://localhost:5001/swagger
- **Health Check:** https://localhost:5001/health

---

## 🎯 Running Multiple Services

### Option 1: Run Services Individually (Development)

**Terminal 1 - Identity Service:**
```bash
cd src/Services/Identity/Identity.API
dotnet run
# Running on: https://localhost:5001
```

**Terminal 2 - Student Service:**
```bash
cd src/Services/Student/Student.API
dotnet run
# Running on: https://localhost:5002
```

**Terminal 3 - Teacher Service:**
```bash
cd src/Services/Teacher/Teacher.API
dotnet run
# Running on: https://localhost:5003
```

### Option 2: Run with Docker Compose (Full Stack)
```bash
# TODO: Add Dockerfile to each service
# Then run:
docker-compose -f docker-compose.full.yml up -d
```

### Option 3: Run in Visual Studio (Easiest)
1. Open `EMIS.sln` in Visual Studio
2. Right-click on solution → **Set Startup Projects**
3. Select **Multiple startup projects**
4. Choose services to run (Identity.API, Student.API, etc.)
5. Click **Start** (F5)

---

## 📝 Basic Usage Examples

### 1. Test Identity Service

**Register a new user:**
```bash
curl -X POST "https://localhost:5001/api/v1/auth/register" \
  -H "Content-Type: application/json" \
  -d '{
    "tenantId": "school-demo",
    "username": "teacher@demo.com",
    "password": "Test@123",
    "email": "teacher@demo.com",
    "fullName": "Demo Teacher",
    "phoneNumber": "0912345678",
    "role": "Teacher"
  }'
```

**Login:**
```bash
curl -X POST "https://localhost:5001/api/v1/auth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "tenantId": "school-demo",
    "username": "teacher@demo.com",
    "password": "Test@123"
  }'
```

**Response:**
```json
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIs...",
    "refreshToken": "refresh_token_here",
    "expiresIn": 3600,
    "user": {
      "userId": "uuid",
      "username": "teacher@demo.com",
      "fullName": "Demo Teacher",
      "roles": ["Teacher"]
    }
  }
}
```

### 2. Test Student Service

**Create a student (requires authentication):**
```bash
curl -X POST "https://localhost:5002/api/v1/students" \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "code": "2024-001",
    "fullName": "Bé Minh An",
    "gender": "Male",
    "dateOfBirth": "2021-05-15",
    "ethnicity": "Kinh",
    "address": {
      "street": "123 Đường ABC",
      "ward": "Phường 1",
      "district": "Quận 1",
      "city": "TP. HCM"
    },
    "classId": "class-uuid",
    "parents": [
      {
        "fullName": "Nguyễn Văn A",
        "phoneNumber": "0912345678",
        "relationship": "Father",
        "isPrimary": true
      }
    ]
  }'
```

---

## 🗄️ Database Setup

### MySQL
```bash
# Connect to MySQL
docker exec -it emis-mysql mysql -uroot -pEMISPassword123!

# Create databases for each service
CREATE DATABASE emis_identity;
CREATE DATABASE emis_student;
CREATE DATABASE emis_teacher;
# ... etc

# Exit
exit
```

### MongoDB
```bash
# Connect to MongoDB
docker exec -it emis-mongodb mongosh -u admin -p EMISPassword123!

# Switch to database
use emis_chat

# Create collection
db.createCollection("messages")

# Exit
exit
```

### Redis
```bash
# Connect to Redis CLI
docker exec -it emis-redis redis-cli -a EMISPassword123!

# Test Redis
SET test "Hello EMIS"
GET test

# Exit
exit
```

---

## 🧪 Running Tests

### All Tests
```bash
# Run all tests
dotnet test

# Run with code coverage
dotnet test /p:CollectCoverage=true /p:CoverageReportFormat=opencover
```

### Unit Tests Only
```bash
cd tests/UnitTests
dotnet test
```

### Integration Tests Only
```bash
cd tests/IntegrationTests
dotnet test
```

---

## 🐛 Troubleshooting

### Issue: Port already in use
```bash
# Find process using port 5001
lsof -i :5001  # macOS/Linux
netstat -ano | findstr :5001  # Windows

# Kill the process
kill -9 <PID>  # macOS/Linux
taskkill /PID <PID> /F  # Windows
```

### Issue: Docker containers not starting
```bash
# Stop all containers
docker-compose down

# Remove all containers and volumes
docker-compose down -v

# Restart
docker-compose up -d

# Check logs
docker-compose logs -f
```

### Issue: Build errors
```bash
# Clean solution
dotnet clean

# Restore packages
dotnet restore --force

# Rebuild
dotnet build
```

### Issue: Cannot connect to MySQL
```bash
# Check MySQL is running
docker ps | grep mysql

# Check MySQL logs
docker logs emis-mysql

# Restart MySQL
docker restart emis-mysql
```

---

## 📂 Project Structure Quick Reference

```
emis-clean-architecture-ddd/
├── src/
│   ├── BuildingBlocks/          # Shared libraries
│   ├── Services/                # 13 microservices
│   │   ├── Identity/
│   │   ├── Student/
│   │   ├── Teacher/
│   │   └── ... (10 more)
│   └── ApiGateway/              # API Gateway
├── tests/                       # Test projects
├── docs/                        # Documentation
├── scripts/                     # Utility scripts
├── docker-compose.yml           # Infrastructure
└── EMIS.sln                     # Solution file
```

---

## 🎓 Next Steps

### For Developers
1. 📖 Read [SOLUTION_STRUCTURE.md](./SOLUTION_STRUCTURE.md) for detailed architecture
2. 🎯 Read [02-Microservices-Design.md](./docs/02-Microservices-Design.md) for service details
3. 💻 Start implementing domain models in `*.Domain` projects
4. 🧪 Write unit tests as you develop
5. 📝 Update API documentation in Swagger

### For Architects
1. 📊 Review [01-System-Overview.md](./docs/01-System-Overview.md)
2. 🗄️ Review [03-Domain-Models-and-Database.md](./docs/03-Domain-Models-and-Database.md)
3. 🔧 Review [05-Technology-Stack.md](./docs/05-Technology-Stack.md)
4. 🚀 Review [06-Deployment-Architecture.md](./docs/06-Deployment-Architecture.md)

### For DevOps
1. 🐳 Review `docker-compose.yml` for infrastructure
2. ☸️ Review Kubernetes manifests in [06-Deployment-Architecture.md](./docs/06-Deployment-Architecture.md)
3. 📊 Setup monitoring (Prometheus + Grafana)
4. 📝 Setup logging (ELK Stack)
5. 🔒 Configure security (Secrets, SSL/TLS)

---

## 📞 Getting Help

### Documentation
- 📚 [Main README](./README.md)
- 🏗️ [Solution Structure](./SOLUTION_STRUCTURE.md)
- 📖 [Full Documentation](./docs/)

### Community
- 💬 Slack: emis-team.slack.com
- 🐛 Issues: GitHub Issues
- 📧 Email: support@emis.com

---

## ✅ Checklist

Before starting development, ensure:

- [ ] .NET 8.0 SDK installed
- [ ] Docker Desktop running
- [ ] All infrastructure services running (`docker-compose ps`)
- [ ] Solution builds successfully (`dotnet build`)
- [ ] All tests pass (`dotnet test`)
- [ ] Can access Swagger UI on services
- [ ] MySQL, MongoDB, Redis are accessible
- [ ] RabbitMQ Management UI accessible (http://localhost:15672)
- [ ] Read architecture documentation
- [ ] Understand Clean Architecture layers
- [ ] Understand DDD concepts

---

**🎉 Congratulations! You're ready to start developing EMIS!**

Happy coding! 🚀
