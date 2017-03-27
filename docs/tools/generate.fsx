// --------------------------------------------------------------------------------------
// Builds the documentation from `.fsx` and `.md` files in the 'docs/content' directory
// (the generated documentation is stored in the 'docs/output' directory)
// --------------------------------------------------------------------------------------

// Binaries that have XML documentation (in a corresponding generated XML file)
let referenceBinaries = [ "FSharpVSPowerTools.Core.dll" ]
// Web site location for the generated documentation
let website = "/Episerver.Basis.Slim"

let githubLink = "http://github.com/Arthyon/Episerver.Basis.Slim"

// Specify more information about your project
let info =
  [ "project-name", "Creuna Basis Slim"
    "project-author", "Christian Hochlin"
    "project-summary", "A quickstart template for new Episerver projects"
    "project-github", githubLink
    "project-releases", githubLink + "/releases"
    "project-nuget", ""
    ]
// --------------------------------------------------------------------------------------
// For typical project, no changes are needed below
// --------------------------------------------------------------------------------------

#load "../../packages/build/FSharp.Formatting/FSharp.Formatting.fsx"
#r "../../packages/build/FAKE/tools/NuGet.Core.dll"
#r "../../packages/build/FAKE/tools/FakeLib.dll"
open Fake
open System.IO
open Fake.FileHelper
open FSharp.Literate
open FSharp.MetadataFormat

open Fake
open System.IO
open Fake.FileHelper
open FSharp.Literate
open FSharp.MetadataFormat

// When called from 'build.fsx', use the public project URL as <root>
// otherwise, use the current 'output' directory.
#if RELEASE
let root = website
#else
let root = "file://" + (__SOURCE_DIRECTORY__ @@ "../output")
#endif

// Paths with template/source/output locations
let content    = __SOURCE_DIRECTORY__ @@ "../content"
let output     = __SOURCE_DIRECTORY__ @@ "../output"
let files      = __SOURCE_DIRECTORY__ @@ "../files"
let templates  = __SOURCE_DIRECTORY__ @@ "templates"
let formatting = __SOURCE_DIRECTORY__ @@ "../../packages/build/FSharp.Formatting/"
let docTemplate = formatting @@ "templates/docpage.cshtml"

// Where to look for *.csproj templates (in this order)
let layoutRootsAll = new System.Collections.Generic.Dictionary<string, string list>()
layoutRootsAll.Add("en",[ templates; formatting @@ "templates"
                          formatting @@ "templates/reference" ])
subDirectories (directoryInfo templates)
|> Seq.iter (fun d ->
                let name = d.Name
                if name.Length = 2 || name.Length = 3 then
                    layoutRootsAll.Add(
                            name, [templates @@ name
                                   formatting @@ "templates"
                                   formatting @@ "templates/reference" ]))

// Copy static files and CSS + JS from F# Formatting
let copyFiles () =
  CopyRecursive files output true |> Log "Copying file: "
  ensureDirectory (output @@ "content")
  CopyRecursive (formatting @@ "styles") (output @@ "content") true 
    |> Log "Copying styles and scripts: "



// Build documentation from `fsx` and `md` files in `docs/content`
let buildDocumentation () =
  
  // First, process files which are placed in the content root directory.

  Literate.ProcessDirectory
    ( content, docTemplate, output, replacements = ("root", root)::info,
      layoutRoots = layoutRootsAll.["en"],
      generateAnchors = true,
      processRecursive = false)

  // And then process files which are placed in the sub directories
  // (some sub directories might be for specific language).

  let subdirs = Directory.EnumerateDirectories(content, "*", SearchOption.TopDirectoryOnly)
  for dir in subdirs do
    let dirname = (new DirectoryInfo(dir)).Name
    let layoutRoots =
        // Check whether this directory name is for specific language
        let key = layoutRootsAll.Keys
                  |> Seq.tryFind (fun i -> i = dirname)
        match key with
        | Some lang -> layoutRootsAll.[lang]
        | None -> layoutRootsAll.["en"] // "en" is the default language

    Literate.ProcessDirectory
      ( dir, docTemplate, output @@ dirname, replacements = ("root", root)::info,
        layoutRoots = layoutRoots,
        generateAnchors = true )

// Generate
copyFiles()
buildDocumentation()
