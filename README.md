Steps to update
==============

a) Update JAM group name patterns if needed. The only byte that should need an update is the first one. That specific byte is right before the first character of the group name.
If you find a new JAM group, add it to the JamGroup enumeration.

b) Make sure the pattern for CMSG CliPutWithMsgId did not change. Look for a ctor in a LUA API function to find one.

c) Make sure no section was added. If it was, you will need to adjust the increment on nameOffset in JamDispatch's CTOR.
![This is how you find it.](./dumperthing.png)

d) Find the BeaEngine DLL you'll need. Update bindings if needed.

e) Run the damn thing.

  TODO
=========
- Implement SMSG diffing correctly
- Fix Bad/Ugly code
