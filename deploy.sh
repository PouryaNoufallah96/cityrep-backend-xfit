#!/bin/bash




IMAGE_NAME="cityrep.api"
CONTAINER_NAME="api.cityrep.ir"

# Load environment variables for docker-compose interpolation.
if [ -f .env ]; then
  echo "---------Loading environment variables from .env..."
  set -a
  # shellcheck disable=SC1091
  source .env
  set +a
else
  echo "WARNING: .env not found — docker-compose variables will be empty."
fi

# Host bind mounts used by compose (images + mongo data).
mkdir -p /home/ubuntu/images /home/ubuntu/mongodb

echo "---------Building and publishing the project..."
dotnet build XFit/XFit.csproj -c Release
dotnet publish XFit/XFit.csproj -c Release -o publish

echo "---------Building Docker image..."
docker build -t $IMAGE_NAME .

echo "---------Stopping old containers if exist..."
docker-compose down


echo "---------Starting containers (mongo + api)..."
docker-compose up -d


echo "---------Container status:"
docker-compose ps

echo "List of all containers:"
docker ps -a

echo "Showing logs (press Ctrl+C to exit)..."
docker-compose logs -f
