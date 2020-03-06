build_packages:
	dotnet pack -o ./dist
push:
	$(foreach file, $(wildcard ./dist/*.nupkg), dotnet nuget push $(file) -k oy2gudfigccpycyxj2sakjafndb7pm3ehdfk5kkmdfokku -s https://api.nuget.org/v3/index.json;)
