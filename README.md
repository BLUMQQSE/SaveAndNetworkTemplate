# LevelManager

All Godot scene saved within the levels folder will be marked as a "Level". A Level acts as a scene which can be placed under the "Main" scene as a child,
and contain child nodes of its own. Whenever a level is saved, all nodes that are within it will be saved aswell into one Json file. 

When a game is run in the Godot engine, on start all level scenes will be loaded and converted to json, then saved to a default save file "Static" in the User path.
When a game is run as an executable, the ResourceManager.json file will be used to collect all level json files, and again will create a "Static" Save.

> Level expects a Player scene to be named "Player". Note Player is not a level itself.


# SaveManager

The save system is used for saving levels and players. Writing to, and reading from, files is performed asynchoronously. This system should not need interacted with directly
from scripts. Instead the LevelManager should be used for instantiating levels, and NetworkDataManager should be used for instantiating nodes. 
> Save Manager expects a string StartUpScene for which the player will default into on first launching a new save for the game.

# NetworkManager

The networking system is used for communicating between a server and client. By default, when running a game a user acts as a server until becoming a client to someone
elses game. NetworkManager provides information regarding whether the user is a server, what their OwnerId is, and some other useful information.

# NetworkDataManager

 The NetworkDataManager provides methods for interacting with data handled on the server. The most important of which being the AddServerNode(), RemoveServerNode(), AddSelfNode(), RemoveSelfNode().
 These methods provide wrappers for adding nodes to the scene tree, while also informing the network what to do about it. Client and Server both have access to Add/RemoveSelfNode(), and
 this will add a node only to the local machine. The NetworkDataManager also provides an RpcServer method for referencing nodes on the server.
 > Rpc calls to the server will only work on methods with parameters of type Variant. Rpc relies on Godot's "Call" method, which uses Variants.

 # ResourceManager

 Acts as a Dictionary for all audio, visual, script, resources, storing their name as a key and their filepath as a value.
 This file will update on ready when run in the editor, and is saved to a json file for when running from an executable.
