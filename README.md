# HookTrigger
![Worker Docker image](https://github.com/mihaimyh/HookTrigger/workflows/Build%20Worker%20Docker%20image/badge.svg)

![API Docker image](https://github.com/mihaimyh/HookTrigger/workflows/Build%20API%20Docker%20image/badge.svg)

An application using AspNetCore Web API which exposes and endpoint to listen for Docker Hub webhooks.

When a new Docker image is pushed on Docker hub the API will receive a webhook notification, publish it to a Kafka topic from where a worker service app will pick it, find the corresponding kubernetes pod and restart it to update the to latest image.

This project is just for demo purposes.
