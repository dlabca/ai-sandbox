# AI Sandbox 3D

A fun 3D sandbox game built with MonoGame and C#. Mine resources, craft tools, build structures, and explore a world populated with various animals.

## Features

- **3D Sandbox World**: Explore a procedurally generated terrain with different block types
- **Mining System**: Extract wood logs from trees and stone from the ground
- **Crafting System**: Combine resources to create planks, sticks, and tools
- **Building**: Place blocks to build structures in the world
- **Dynamic Inventory**: Track and manage your collected items
- **AI Animals**: Interactive animals that wander around the world
  - Sheep (White)
  - Pigs (Orange)
  - Cows (Black)
  - Chickens (Yellow)
- **Free Exploration**: Move freely in a 3D environment with jumping physics

## Controls

- **WASD** - Move character
- **Space** - Jump
- **E** - Mine blocks (wood)
- **B** - Build with logs (hold)
- **C** - Toggle crafting menu
- **UP/DOWN** - Navigate crafting recipes (when menu open)
- **ENTER** - Craft selected recipe
- **F11** - Toggle fullscreen

## Building

### Requirements
- .NET 8.0 SDK
- MonoGame 3.8.1

### Build & Run
```bash
dotnet restore
dotnet build -c Release
dotnet run
```

### Publish
```bash
dotnet publish -c Release -r win-x64 --self-contained false
```

## Game Mechanics

### Mining
Look at wood blocks and press **E** to mine them. You'll receive logs that can be used for crafting or building.

### Crafting
Press **C** to open the crafting menu. Navigate with UP/DOWN arrows and press ENTER to craft:
- **Wood Log** → **4x Wood Planks**
- **Wood Plank** → **2x Sticks**
- **2x Logs + 3x Sticks** → **Wooden Axe**
- **3x Stone + 3x Sticks** → **Pickaxe**

### Building
Hold **B** to place wooden blocks in the world. You'll need logs in your inventory.

## Technical Details

- Built with MonoGame.Framework.DesktopGL
- Pure 3D rendering with vertex buffers
- Simple physics system with gravity and collision
- AI behavior using random walk patterns
- State-based inventory and crafting system

## License

MIT
