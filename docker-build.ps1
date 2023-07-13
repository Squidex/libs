$version= "1.3.0"

## Calculate Major version
$version_major = $version.split(".",3)[0]

Write-Output "Version $version, Major Version: $version_major"

docker build . -t squidex/resizer:latest -t squide/resizer:$version -t squide/resizer:$version_major
docker push squidex/resizer:latest
docker push squidex/resizer:$version
docker push squidex/resizer:$version_major