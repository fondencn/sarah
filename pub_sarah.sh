# Build the server backend Docker image
docker build -t sarah-server:latest -f Dockerfile.sarah-server .

# Tag the Docker image for the remote host
docker tag sarah-server:latest pi:6000/sarah-server:latest

# Push the Docker image to the remote host
docker push pi:6000/sarah-server:latest

# Build the client Docker image
docker build -t sarah-client:latest -f Dockerfile.sarah-client .

# Tag the Docker image for the remote host
docker tag sarah-client:latest pi:6000/sarah-client:latest

# Push the Docker image to the remote host
docker push pi:6000/sarah-client:latest
