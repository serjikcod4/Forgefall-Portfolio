# Hex power network

Read [HexCoordinate.cs](../Samples/Forge/HexCoordinate.cs), then [RelayConnectivity.cs](../Samples/Forge/RelayConnectivity.cs).

Each source covers a disc of logical axial hex cells, clipped by the supplied arena predicate. Two regions connect if they overlap or if a cell in one shares an edge with a cell in the other. Visual proximity alone does not define connectivity.

The resolver traverses the source graph with a queue starting at the living Forge. A disconnected group of relays, including a closed cycle, contributes no powered cells. After traversal, the result contains connected-source flags, the union of their powered cells and the number of connected sources covering each cell. Coverage counts support the Resonant Grid mechanic without adding research-specific rules to the resolver.

## Integration

The full game's construction layer runs this computation on topology changes. It also simulates a proposed new relay or a source removal using the same resolver. This makes preview and committed power follow the same rules. Selling a bridge can disconnect downstream buildings; restoring a path restores their power without resetting turret cooldowns.

## Test cases

The [EditMode fixture](../Tests/Editor/RelayConnectivityTests.cs) adapts existing project checks for overlap, edge contact, non-contact, an isolated cycle and alternate paths. It adds explicit coverage-count and dead-Forge cases for the exported resolver. The [validation notes](validation.md) distinguish original historical evidence from checks performed for this publication.

## Tradeoffs

The implementation materializes a set of covered cells per source, then compares candidate regions during breadth-first traversal. It is straightforward to inspect and adequate for small networks, but can approach quadratic source comparisons. Large networks could benefit from a cell-to-source index.

The result exposes `PoweredCells` as a mutable HashSet. Consumers must treat it as a snapshot; a read-only boundary would make that contract more explicit.
