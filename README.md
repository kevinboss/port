# port

[![CI](https://github.com/kevinboss/port/actions/workflows/ci.yaml/badge.svg?event=push)](https://github.com/kevinboss/port/actions/workflows/ci.yaml)


## What is it?

port, the name originating from the idea that only one container may dock simultaneously, is a tool that has been designed to manage multiple images and / or tags of these images.
It allows the user to run one of these images / tags in a container, creating snapshots of that running container and manage the downloaded images.

## How to get it?

*Install using scoop*

`scoop bucket add maple 'https://github.com/kevinboss/maple.git'`

`scoop install port`

*Install manually*

[Latest release 💾](https://github.com/kevinboss/port/releases/latest)

Then add folder to path `$Env:PATH += ";C:\Path\To\Folder"`

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

A default .port file will be created in your user profile of you don't manually create one

## Commands

### run \[identifier\]

Allows the user the run a specified tag (base or snapshot) of an image.

identifier is optional, if not provided the user will be asked to select an image.

### list \[identifier\]

Lists all images with their respective tags.

identifier is optional, if not provided all images will be listed. If provided all images belonging to the same identifier will be listed.

### commit -t(ag)

Creates an image from the currently running container.

-t(ag) is optional, if not provied current date-time will be used as the tag.

### reset

Terminates and removes the currently running container. Then recreates the container using the image the running container was using.

### remove \[identifier\]

Allows the user the delete a specified tag (base, snapshot or untagged) of an image.

identifier is optional, if not provided the user will be asked to select an image.

### pull \[identifier\]

Allows the user the pull a specified tag (base or snapshot) of an image.

identifier is optional, if not provided the user will be asked to select an image.
