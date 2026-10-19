# RT update v266.0.0 -> v293.0.1 TODO

Branch: rt-update-293-0-1 (based on agents-docs). Markers: `// MAID rt-update`.

- [x] Read RT release notes 267..293
- [ ] Stage 1: submodule -> v293.0.1, global.json -> .NET 10 SDK
- [ ] Stage 2: .sln -> .slnx + SolutionGen, csproj TFM/LangVersion, Directory.Packages.props, Imports/*.props instead of direct RT refs
- [ ] Stage 3: restore OK
- [ ] Stage 4: build Shared -> Client/Server, fix mechanical errors
- [ ] Stage 5: Goob projects, Tests/IntegrationTests/Tools/etc projects compile
- [ ] Stage 6: CI workflows / docs / scripts (.NET 9 -> 10)
- [ ] Stage 7: controversial list (RT_UPDATE/CONTROVERSIAL.md)
