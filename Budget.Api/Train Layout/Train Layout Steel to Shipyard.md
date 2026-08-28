```mermaid
flowchart TD
%% ============================================================
%% HIDDEN STAGING (BEHIND 17' WALL)
%% ============================================================
subgraph STG ["Hidden Staging (18 #quot; Deep, Behind Backdrop)"]
direction LR
ST1((Staging 1))
ST2((Staging 2))
ST3((Staging 3))
ST4((Staging 4))
ST5((Staging 5))
ST6((Staging 6))
end
%% ============================================================
%% SCENIC WALL (DOUBLE TRACK MAINLINE)
%% ============================================================
subgraph WALL ["17' Scenic Wall – Double Track Mainline"]
direction LR
ML1A((Mainline Track 1))
ML1B((Mainline Track 2))
TOWN((Rural Town & Depot))
FARM((Farm & Fields))
end
%% ============================================================
%% STEEL MILL PENINSULA
%% ============================================================
subgraph MILL ["Steel Mill Peninsula (4' Wide)"]
direction TB
%% Mill Lead  
MLEAD((Mill Lead – Single Track))

%% Yard Ladder  
subgraph YARD ["Mill Yard"]
direction TB  
    Y1((Yard Track 1))  
    Y2((Yard Track 2))  
    Y3((Yard Track 3))  
    Y4((Yard Track 4))  
end

%% Coke Ovens  
CO((Coke Ovens))

%% Blast Furnace Complex  
subgraph BF ["Blast Furnace Complex"]
direction TB  
    HF((Highline))  
    BFURN((Blast Furnace))  
    HOT((Hot Metal Track))  
    SLAG((Slag Track))  
end

%% Rolling Mill  
RM((Rolling Mill – Finished Steel))
end
%% ============================================================
%% SHIPYARD PENINSULA
%% ============================================================
subgraph SHIP ["Shipyard Peninsula (4' Wide)"]
direction TB
SYLEAD((Shipyard Lead – Single Track))

%% Ore Dock  
subgraph OREDOCK ["Ore Dock & Unloader"]
direction TB  
    ORE1((Ore Unloader))  
    ORE2((Ore Yard Track))  
end

%% Slipways  
subgraph SLIPWAYS ["Shipbuilding Slipways"]
direction TB  
    SL1((Slipway 1))  
    SL2((Slipway 2))  
end

%% Long Spurs  
subgraph LONGSPURS ["Long Delivery Spurs"]
direction TB  
    LS1((Steel Delivery Spur 1))  
    LS2((Steel Delivery Spur 2))  
    LS3((Machinery Spur))  
end
end
%% ============================================================
%% CONNECTIONS
%% ============================================================
%% Staging to Scenic Wall
ST1 --> ML1A
ST2 --> ML1B
ST3 --> ML1A
ST4 --> ML1B
ST5 --> ML1A
ST6 --> ML1B
%% Scenic Wall Flow
ML1A --> TOWN --> FARM --> ML1A
ML1B --> TOWN --> FARM --> ML1B
%% Scenic Wall to Steel Mill Peninsula
ML1A --> MLEAD
MLEAD --> Y1
MLEAD --> Y2
MLEAD --> Y3
MLEAD --> Y4
%% Yard to Coke Ovens
Y1 --> CO
%% Yard to Blast Furnace Complex
Y2 --> HF --> BFURN --> HOT --> SLAG
%% Yard to Rolling Mill
Y3 --> RM
%% Scenic Wall to Shipyard Peninsula
ML1B --> SYLEAD
%% Shipyard Branching
SYLEAD --> ORE1 --> ORE2
SYLEAD --> SL1
SYLEAD --> SL2
SYLEAD --> LS1
SYLEAD --> LS2
SYLEAD --> LS3
%% Return Loop
ML1A --> ML1B
ML1B --> ST1
```
