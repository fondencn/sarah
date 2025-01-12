# Build the Docker image
docker build -t sarah-location-server:latest -f Dockerfile.sarah-location-server .

# Tag the Docker image for the remote host
docker tag sarah-location-server:latest pi:6000/sarah-location-server:latest

# Push the Docker image to the remote host
docker push pi:6000/sarah-location-server:latest
