# RemoteRcon
Remote Rcon for Rust servers using umod

## Permissions
`remotercon.storedexecute` and `remotercon.execute`

## Commands
### remotercon.storedexecute (name, args...)
Runs a stored command via the configuration file. Safest method.

### remotercon.execute (address, port, password, args...)
Extra arguments will be used as the command and will be joined for you.


## Configuration
```JSON
{
  "LogMessages": true,
  "RemoteCommands": [
    {
      "CommandName": "AddVIP",
      "Commands": [
        "oxide.group add vip",
        "oxide.usergroup add {0} vip"
      ],
      "Servers": [
        "Local",
        "Local_Duplicate"
      ]
    }
  ],
  "RemoteServers": [
    {
      "Address": "127.0.0.1",
      "Name": "Local",
      "Password": "YourSuperRconPassword",
      "Port": "28016"
    },
    {
      "Address": "127.0.0.1",
      "Name": "Local_Duplicate",
      "Password": "YourSuperRconPassword",
      "Port": "28016"
    }
  ]
}
```

## TODO
Add filters to stored commands so the server can attempt to convert a steam ID to a player name if possible.

## Notes
This will not listen for a response from the server, and most servers will not show anything in the console.
