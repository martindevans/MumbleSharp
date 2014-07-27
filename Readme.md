## MumbleSharp

MumbleSharp is an implementation of a mumble client in C#.

The solution comes in two parts:

 - MumbleSharp is the actual MumbleSharp library which is a class library for building mumble clients.
 - MumbleClient is a mumble client, currently just a console application to use for testing.

As you can see from the MumbleClient Program.cs creating a new client is very simple:

 1. Implement IMumbleProtocol and implement methods to respond to messages of different types however you wish.
 2. Use a MumbleConnection to connect to a server.

## Work In Progress
 
This project is currently only partly functional! The client supports almost everything a mumble client can do *except* for voice - voice packets are received but are not currently decoded.

## Contributing

I'm only occasionally working on MumbleSharp in my spare time but I'm very happy to receive contributions. If you're thinking of contributing ping me an email (martindevans@gmail.com) and I'll try to give you any advice I can to achieve whatever you want.