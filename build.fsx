#r "paket:
version 7.0.2
framework: net6.0
source https://api.nuget.org/v3/index.json

nuget Microsoft.Build 17.3.2
nuget Microsoft.Build.Framework 17.3.2
nuget Microsoft.Build.Tasks.Core 17.3.2
nuget Microsoft.Build.Utilities.Core 17.3.2

nuget Be.Vlaanderen.Basisregisters.Build.Pipeline 6.0.6 //"

#load "packages/Be.Vlaanderen.Basisregisters.Build.Pipeline/Content/build-generic.fsx"

open Fake
open Fake.Core
open Fake.Core.TargetOperators
open Fake.IO
open Fake.IO.FileSystemOperators
open ``Build-generic``

let product = "Basisregisters Vlaanderen"
let copyright = "Copyright (c) Vlaamse overheid"
let company = "Vlaamse overheid"

let dockerRepository = "building-registry"
let assemblyVersionNumber = (sprintf "2.%s")
let nugetVersionNumber = (sprintf "%s")

let buildSolution = buildSolution assemblyVersionNumber
let buildSource = build assemblyVersionNumber
let buildTest = buildTest assemblyVersionNumber
let setVersions = (setSolutionVersions assemblyVersionNumber product copyright company)
let test = testSolution
let publishSource = publish assemblyVersionNumber
let pack = pack nugetVersionNumber
let containerize = containerize dockerRepository
let push = push dockerRepository

supportedRuntimeIdentifiers <- [ "msil"; "linux-x64" ]

// Solution -----------------------------------------------------------------------

Target.create "Restore_Solution" (fun _ -> restore "Grb")

Target.create "Build_Solution" (fun _ ->
  setVersions "SolutionInfo.cs"
  buildSolution "Grb")

Target.create "Test_Solution" (fun _ ->
    [
        "test" @@ "Grb.Building.Tests"
    ] |> List.iter testWithDotNet
)

Target.create "Publish_Solution" (fun _ ->
  [
    "Grb.Building.Api"
    "Grb.Building.Processor.Upload"
    "Grb.Building.Processor.Job"
  ] |> List.iter publishSource)

Target.create "Pack_Solution" (fun _ ->
  [
  ] |> List.iter pack)

Target.create "Containerize_ApiGrb" (fun _ -> containerize "Grb.Building.Api" "api-grb")
Target.create "Containerize_GrbProcessorUpload" (fun _ -> containerize "Grb.Building.Processor.Upload" "grb-processor-upload")
Target.create "Containerize_GrbProcessorJob" (fun _ -> containerize "Grb.Building.Processor.Job" "grb-processor-job")

Target.create "SetAssemblyVersions" (fun _ -> setVersions "SolutionInfo.cs")
// --------------------------------------------------------------------------------

Target.create "Build" ignore
Target.create "Test" ignore
Target.create "Publish" ignore
Target.create "Pack" ignore
Target.create "Containerize" ignore

"NpmInstall"
  ==> "DotNetCli"
  ==> "Clean"
  ==> "Restore_Solution"
  ==> "Build_Solution"
  ==> "Build"

"Build"
  ==> "Test_Solution"
  ==> "Test"

"Test"
  ==> "Publish_Solution"
  ==> "Publish"

"Publish"
  ==> "Pack_Solution"
  ==> "Pack"

"Pack"
  ==> "Containerize"
// Possibly add more projects to containerize here

// By default we build & test
Target.runOrDefault "Test"
