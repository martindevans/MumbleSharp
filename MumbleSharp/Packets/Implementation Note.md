mumble.cs is generated automatically (using the protobuf-net protogen tool) from [mumble.proto](https://github.com/mumble-voip/mumble/blob/master/src/Mumble.proto)

Use the "detect missing" feature to generate useful optional properties:

    PS C:\SomePath> ./protogen -i:mumble.proto -o:Mumble.cs -p:detectMissing