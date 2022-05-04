# dcma (DoCker ManAger)

[![CI](https://github.com/kevinboss/dcma/actions/workflows/ci.yaml/badge.svg?event=push)](https://github.com/kevinboss/dcma/actions/workflows/ci.yaml)

[Latest release 💾](https://github.com/kevinboss/dcma/releases/latest)

## Why?

Docker is used deploy various releases of a database to the development machines. Developers can access the different releases by loading different image tags.
In addition there is a need to update those images (new versions get created daily) as well as creating snapshots of the current state of the database.
It is also nice to be able to clean up not needed images to regain disk-space.

## What is it?

dcma allows the user to configure multiple images and assign a readable identifier for each image.
Afterwards the user can run the images (this also shuts down any other running containers using a configured image), create snapshots of the running containers as well as remove the original image as well as the snapshots.

## Configuration

!TODO

## Commands

### run \[identifier\]

identifier is optional, if not provided the user will be asked to select an image.

### list \[identifier\]

identifier is optional, if not provided all images will be listed. If provided all images belonging to the same identifier will be listed.

### commit -t(ag)

-t(ag) is optional, if not provied current date-time will be used as the tag.

### remove \[identifier\]

identifier is optional, if not provided the user will be asked to select an image.
