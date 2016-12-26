# Advanced-Warpplates
## Version 2.0.0.0

Introduction
-----
Warpplates function similarly to teleporters without the need for wiring or teleporters themselves. For basic functionality, simply add two warpplates, set the destination for one (or both for two-way travel), and you're done. You can optionally adjust other functionality such as the area of influence and the delay before being warped.

Commands
-----
`/warpplate add <name>`<br />
Adds a warpplate at the user's location using the specified warpplate name.

`/warpplate del <name>`<br />
Deletes the specified warpplate.

`/warpplate info <name>`<br />
Returns information about the specified warpplate.

`/warpplate mod <name> name <new name>`<br />
Modifies the specified warpplate's name. (Note that this will break any warpplates that use this warpplate as a destination.)

`/warpplate mod <name> size <w,h>`<br />
Modifies the specified warpplate's area. Use the format `width,height` without spaces (example: `warpplate mod spawn size 5,4`).

`/warpplate mod <name> delay <delay>`<br />
Modifies the specified warpplate's delay. The delay is in seconds.

`/warpplate mod <name> destination <dest name>`<br />
Modifies the specified warpplate's destination warpplate.

`/togglewarpplates`<br />
Toggles automatic warpping for the user.

Permissions
-----
`warpplate.set`<br />
Allows use of `/warpplate` command.

`warpplate.use`<br />
Allows use of /togglewarpplates command.<br />
Allows the ability to warp via warpplate.

Database
-----
_Warpplates_

| Column Name | Type | Length |
| --- | --- | --- |
| warppname | VarChar | 30 |
| x | Int32 | 5 |
| y | Int32 | 5 |
| width | Int32 | 5 |
| height | Int32 | 5 |
| delay | Int32 | 3 |
| destination | VarChar | 30 |
| worldid | VarChar | 15 |