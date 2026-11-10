# PackRelease

Builds the CurseForge client zip and the server pack from a CurseForge instance, and uploads them to CurseForge.
Settings are in `packrelease.json` in the instance folder, the CurseForge API token in the `CF_API_TOKEN` environment variable.

## Install / update

From the `MinecraftUtils` folder, after changing the code bump `<Version>` in `PackRelease.csproj`, then:

```
dotnet pack PackRelease -c Release
dotnet tool update -g PackRelease --add-source ./nupkg
```

`tool update` also installs it the first time. Uninstall with `dotnet tool uninstall -g PackRelease`.

## Usage

From the instance folder:

```
packrelease release --dry-run   # build the zips and check the upload, without uploading
packrelease release             # build and upload
packrelease build               # only build the zips
packrelease publish             # only upload the zips already built
packrelease deploy --dry-run    # show what would change on the BisectHosting server
packrelease deploy              # update the server: warning, stop, world archive, SFTP mirror, start
packrelease release --deploy    # build, upload and update the server
```

`deploy` needs rclone, the Bisect API key in `BISECT_HOSTING_API_TOKEN` and the panel password in `BISECT_HOSTING_SFTP_PASSWORD`.

Run `packrelease` without arguments for all the options.
