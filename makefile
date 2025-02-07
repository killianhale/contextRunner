build_packages:
	dotnet pack -o ./dist
push:
	$(foreach file, $(wildcard ./dist/*.nupkg), dotnet nuget push $(file) -k $(shell cat ${keyfile}) -s https://api.nuget.org/v3/index.json;)
