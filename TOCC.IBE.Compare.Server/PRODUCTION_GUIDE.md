# Production Deployment Guide

Complete guide for deploying the TOCC.IBE.Compare.Server in production environments.

## Configuration Strategy

Three-tier priority system (production-ready, cross-platform):

1. **Explicit Path** (highest priority)
2. **TOCC_CONFIG_PATH** environment variable (recommended)
3. **Current Directory** (fallback)

---

## Environment Variable Setup

### Windows

```powershell
# PowerShell (current session)
$env:TOCC_CONFIG_PATH = "C:\app\config"

# Command Prompt
set TOCC_CONFIG_PATH=C:\app\config

# System-wide (requires admin)
setx TOCC_CONFIG_PATH "C:\app\config" /M
```

### Linux/macOS

```bash
# Current session
export TOCC_CONFIG_PATH=/opt/app/config

# Permanent (add to ~/.bashrc or ~/.profile)
echo 'export TOCC_CONFIG_PATH=/opt/app/config' >> ~/.bashrc
source ~/.bashrc
```

---

## Docker Deployment

### Dockerfile

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV TOCC_CONFIG_PATH=/app
ENTRYPOINT ["dotnet", "TOCC.IBE.Compare.Server.dll"]
```

### Docker Compose

```yaml
version: '3.8'
services:
  webserver:
    build: .
    ports:
      - "5000:80"
      - "5001:443"
    environment:
      - TOCC_CONFIG_PATH=/app/config
      - ASPNETCORE_ENVIRONMENT=Production
    volumes:
      - ./config:/app/config:ro
```

### Run Container

```bash
docker run -d \
  -p 5000:80 \
  -e TOCC_CONFIG_PATH=/app/config \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -v $(pwd)/config:/app/config:ro \
  tocc-webserver:latest
```

---

## Kubernetes Deployment

### ConfigMap

```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: tocc-config
data:
  appsettings.json: |
    {
      "IntegrationTest": {
        "Enabled": true,
        "V1BaseUrl": "https://api.example.com/v1",
        "V2BaseUrl": "https://api.example.com/v2"
      }
    }
```

### Deployment

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: tocc-webserver
spec:
  replicas: 3
  selector:
    matchLabels:
      app: tocc-webserver
  template:
    metadata:
      labels:
        app: tocc-webserver
    spec:
      containers:
      - name: webserver
        image: tocc-webserver:latest
        ports:
        - containerPort: 80
        env:
        - name: TOCC_CONFIG_PATH
          value: "/app/config"
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        volumeMounts:
        - name: config
          mountPath: /app/config
          readOnly: true
      volumes:
      - name: config
        configMap:
          name: tocc-config
```

---

## Linux Service (systemd)

### Service File

`/etc/systemd/system/tocc-webserver.service`:

```ini
[Unit]
Description=TOCC IBE Compare Web Server
After=network.target

[Service]
Type=notify
User=www-data
WorkingDirectory=/opt/tocc/webserver
ExecStart=/usr/bin/dotnet /opt/tocc/webserver/TOCC.IBE.Compare.Server.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=TOCC_CONFIG_PATH=/etc/tocc/config

[Install]
WantedBy=multi-user.target
```

### Commands

```bash
sudo systemctl daemon-reload
sudo systemctl start tocc-webserver
sudo systemctl enable tocc-webserver
sudo systemctl status tocc-webserver
sudo journalctl -u tocc-webserver -f
```

---

## Security Best Practices

1. **Never commit secrets** to appsettings.json
2. **Use environment variables** for sensitive data
3. **Mount config as read-only** in containers
4. **Restrict file permissions**: `chmod 600 appsettings.json`
5. **Use secrets management** (Azure Key Vault, AWS Secrets Manager)

### Example with Secrets

```bash
# Override config values via environment variables
export IntegrationTest__V1ApiKey="secret-key"
export IntegrationTest__V2ApiKey="secret-key"
```

---

## Troubleshooting

### Config Not Found

```bash
# Check environment variable
echo $TOCC_CONFIG_PATH

# Verify file exists
ls -la $TOCC_CONFIG_PATH/appsettings.json

# Check permissions
ls -l $TOCC_CONFIG_PATH/appsettings.json
```

### Wrong Configuration Loaded

Configuration priority:
1. Environment variables (highest)
2. appsettings.{Environment}.json
3. appsettings.json (lowest)

```bash
# Check active environment
echo $ASPNETCORE_ENVIRONMENT
```

---

## CI/CD Integration

### GitHub Actions

```yaml
name: Deploy
on:
  push:
    branches: [ main ]

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v2
    - name: Setup .NET
      uses: actions/setup-dotnet@v1
      with:
        dotnet-version: 6.0.x
    - name: Build
      run: dotnet build --configuration Release
    - name: Test
      run: dotnet test --configuration Release
      env:
        TOCC_CONFIG_PATH: ${{ github.workspace }}/TOCC.IBE.Compare.Server
    - name: Publish
      run: dotnet publish -c Release -o ./publish
```

---

## Summary

✅ Cross-platform compatible  
✅ Container-friendly  
✅ No file system traversal  
✅ Environment variable support  
✅ Production-ready  
