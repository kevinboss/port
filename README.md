# dcma

[![CI](https://github.com/kevinboss/dcma/actions/workflows/ci.yaml/badge.svg?event=push)](https://github.com/kevinboss/dcma/actions/workflows/ci.yaml)


## What is it?

dcma allows the user to configure multiple images and assign a readable identifier for each image.
Afterwards the user can run the images (this also shuts down any other running containers using a configured image), create snapshots of the running containers as well as remove the original image as well as snapshots.

## How to get it?

*Install using scoop*

`scoop bucket add maple 'https://github.com/kevinboss/maple.git'`

`scoop install dcma`

*Install manually*

[Latest release 💾](https://github.com/kevinboss/dcma/releases/latest)

## Configuration

```yaml
version: 1.1
dockerEndpoint: unix:///var/run/docker.sock
imageConfigs:
- identifier: Getting.Started
  imageName: docker/getting-started
  imageTags:
  - latest
  - vscode
  ports:
  - 80:80
```

A default .dcma file will be created in your user profile of you don't manually create one

## Commands

### run \[identifier\]

identifier is optional, if not provided the user will be asked to select an image.

### list \[identifier\]

identifier is optional, if not provided all images will be listed. If provided all images belonging to the same identifier will be listed.

### commit -t(ag)

-t(ag) is optional, if not provied current date-time will be used as the tag.

### reset

### remove \[identifier\]

identifier is optional, if not provided the user will be asked to select an image.

### pull \[identifier\]

identifier is optional, if not provided the user will be asked to select an image.
