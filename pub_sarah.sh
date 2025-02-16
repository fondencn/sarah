# Build the Docker image
docker build -t sarah:latest -f Dockerfile.sarah .

# Tag the Docker image for the remote host
docker tag sarah:latest pi:6000/sarah:latest

# Push the Docker image to the remote host
docker push pi:6000/sarah:latest
