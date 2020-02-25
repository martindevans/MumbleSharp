# Generation of mumble.cs #

The mumble.cs file is to be generated using the protobuf-net protogen tool, this from the Mumble's [mumble.proto](https://github.com/mumble-voip/mumble/blob/master/src/Mumble.proto) file.

## Protobuf-Net ##

You may install protobuf-net protogen tool following the instructions here:

https://www.nuget.org/packages/protobuf-net.Protogen/

`dotnet tool install --global protobuf-net.Protogen --version 2.3.17`

Then launch the protogen executable from the command-line, within the MumbleSharp\Packets folder:

    C:\SomePath\MumbleSharp\Packets> ./protogen --csharp_out=. mumble.proto
