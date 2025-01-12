# Build the Docker image
docker build -t sarah-location-server:latest .

# Tag the Docker image for the remote host
docker tag sarah-location-server:latest pi:5000/sarah-location-server:latest

# Push the Docker image to the remote host
docker push pi:5000/sarah-location-server:latest
