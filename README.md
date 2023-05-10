![port](https://socialify.git.ci/kevinboss/port/image?font=KoHo&language=0&logo=https%3A%2F%2Fi.imgur.com%2FKXUk91q.png&name=1&owner=1&pattern=Charlie%20Brown&stargazers=1&theme=Dark)

# port

[![CI](https://github.com/kevinboss/port/actions/workflows/ci.yaml/badge.svg?event=push)](https://github.com/kevinboss/port/actions/workflows/ci.yaml)
[![CI](https://raw.githubusercontent.com/kevinboss/heartbeat/main/badges/kevinboss_port.svg)](https://github.com/kevinboss/heartbeat)

## What is it

port is a tool that has been designed to manage multiple images and / or tags of these images.
It allows the user to run any of these images. It is then possible to create snapshots of the running containers, reset them and switch between snapshots.

## How to get it

### Install using [scoop](https://scoop.sh)

`scoop bucket add maple 'https://github.com/kevinboss/maple.git'`

`scoop install port`

### Install using [winget](https://learn.microsoft.com/en-us/windows/package-manager/winget/) 

![Winget version](https://img.shields.io/badge/dynamic/xml?label=Winget&prefix=v&query=%2F%2Ftr%5B%40id%3D%27winget%27%5D%2Ftd%5B3%5D%2Fspan%2Fa&url=https%3A%2F%2Frepology.org%2Fproject%2Fport%2Fversions)

`winget install kevinboss.port`

### Install manually

[Latest release 💾](https://github.com/kevinboss/port/releases/latest)

Then add folder to path `$Env:PATH += ";C:\Path\To\Folder"`

## How to configure it

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
  environment:
  - DEBUG=1
```

A default .port file will be created in your user profile if you don't manually create one

## Powershell

To get Unicode support in Powershell, add 

`[console]::InputEncoding = [console]::OutputEncoding = [System.Text.UTF8Encoding]::new()`
 
to your $profile.

## How to use it

![Example](https://github.com/kevinboss/port/raw/master/example.gif)

### run \[identifier\] -r(eset)

Allows the user the run a specified tag (base or snapshot) of an image.

identifier is optional, if not provided the user will be asked to select an image.

-r(eset) is optional, if provided will reset the already existing the container (if one exists for the image being run)

### list \[identifier\]

Lists all images with their respective tags.

identifier is optional, if not provided all images will be listed. If provided all images belonging to the same identifier will be listed.

### commit -t(ag) \[identifier\]

Creates an image from the currently running container.

identifier is optional, if not provided the user will be asked to select a container.

-t(ag) is optional, if not provied current date-time will be used as the tag.

### reset \[identifier\]

Terminates and removes the currently running container. Then recreates the container using the image the running container was using.

identifier is optional, if not provided the user will be asked to select a container.

### remove -r(ecursive) \[identifier\]

Allows the user the delete a specified tag (base, snapshot or untagged) of an image.

identifier is optional, if not provided the user will be asked to select an image.

-r(ecursive) is optional, if provided child images will automatically removed, if not an error will be thrown if an image has child images.

### pull \[identifier\]

Allows the user the pull a specified tag (base or snapshot) of an image.

identifier is optional, if not provided the user will be asked to select an image.

### prune \[identifier\]

Allows the user the remove untagged versions of an image.

identifier is optional, if not provided the user will be asked to select an image.

### stop \[identifier\]

Stops the currently running container.

identifier is optional, if not provided all identifiers will be pruned.
