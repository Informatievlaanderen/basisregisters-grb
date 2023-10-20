# 2. GRB Building Separation

Date: 2023-05-23

## Status

Accepted

## Context

We need to process files given by GRB and return a result set (dbf).
GRB will upload the files to us via S3.

## Decision

We will create a separate repository for the GRB Building processors.
Future GRB implementations can also be included here, if and when they send other files.
All data will be saved on a different database and will not interfere with Building data.

## Consequences

We will need to use `BuildingRegistry` packages for our dependencies.
We will need to create a separate deploy pipeline for the GRB processors, which will have a different version than Building.
We will need to call the `BuildingRegistry` API's to send commands.
