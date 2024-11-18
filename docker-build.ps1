$version= "1.4.0"

## Calculate Major version
$version_major = $version.split(".",3)[0]

Write-Output "Version $version, Major Version: $version_major"

docker build -t resizer .

docker tag resizer squidex/resizer:latest
docker tag resizer squidex/resizer:$version
docker tag resizer squidex/resizer:$version_major

docker push squidex/resizer:latest
docker push squidex/resizer:$version
docker push squidex/resizer:$version_major