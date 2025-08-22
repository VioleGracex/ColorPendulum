# TubeManager.cs Documentation

## Short Description

**TubeManager** is the core class for managing the puzzle gameplay in a "tubes" or "columns" match-3 style game in Unity.  
It handles ball stacking, matching and clearing, lid closing (for full tubes), game over logic, and smooth ball animations with DOTween.  
Key gameplay mechanics include:  
- Auto-stacking balls into columns when they're at rest,
- Matching/clearing logic for 3 (or more) in a row (horizontal, vertical, diagonal),
- Losing hearts if a ball "gets stuck" on top of a full tube or doesn't move for a certain threshold,
- Lids closing when a tube is full,
- Using low wall friction for smooth ball sliding,
- Game state management for clean order flow,
- Ability to clear all balls for replayability without reloading the scene.

---

## Frequently Asked Questions (FAQ)

### Q: **How does the TubeManager know when to stack a ball into a column?**
**A:**  
- TubeManager checks for balls inside each column's area using Physics2D.OverlapBoxAll.
- If a ball is inside, moving slower than a defined threshold, and stays at rest for several frames, it triggers stacking.
- Stacking centers the ball in the target slot and disables its physics.

---

### Q: **How does the lid closing system work?**
**A:**  
- Each column has a "lid" (managed by a separate `LidsController`).
- When a column is full (3 balls), if there's no vertical match, the lid closes (`lidsController.CloseLid`), preventing more balls from entering.
- If a vertical match is found, the lid remains open (`lidsController.OpenLid`).

---

### Q: **What happens if a player drops a ball on a closed lid or full column?**
**A:**  
- The ball rests on top, and if it doesn't move for a set threshold (framesAtRestRequired), the ball is destroyed and the player loses a heart.
- This penalizes the player and encourages precise placement.

---

### Q: **How do you animate the balls and make the gameplay feel good?**
**A:**  
- DOTween is used for all ball movements: stacking, shifting down after a clear, and clearing effects.
- All transitions are smooth, and the code waits for all animations before proceeding to the next state.

---

### Q: **What if a ball falls on a wall and doesn't slide into a tube?**
**A:**  
- The wall colliders are set to zero friction, so balls always slide into the column instead of sticking.

---

### Q: **How is the match-3 logic implemented?**
**A:**  
- After a ball is stacked, the code checks for horizontal, vertical, and diagonal matches (using `GridChecker.GetMatchedBalls`).
- If 3 or more are connected in any direction, all matched balls are cleared with animations, and score is awarded (with multipliers for larger matches).

---

### Q: **How does the game handle the order of operations after a ball is dropped?**
**A:**  
- **Flow:**
    1. Ball is dropped and comes to rest.
    2. Ball is stacked into the lowest available slot in its column.
    3. Check for matches (horizontal/vertical/diagonal).
    4. Remove matched balls (with animation).
    5. Add score (with multiplier for 5+).
    6. Shift all balls down to fill gaps.
    7. Update lids (open/close as needed).
    8. Check for full grid.
    9. If grid is full and no more matches, trigger Game Over.

---

### Q: **How is replayability handled without reloading the scene?**
**A:**  
- `TubeManager.ClearAllBalls()` destroys all ball GameObjects, resets all state, and clears the grid.
- This allows restarting the game or replaying without a scene reload.

---

### Q: **How does the code prevent multiple matches from being missed (cascading matches)?**
**A:**  
- After each clear and shift, the code checks the whole grid for any new matches, and recursively clears them before checking for game over.

---

### Q: **How does TubeManager use GameManager and game states?**
**A:**  
- Game state is checked at the start of every `Update()`; only runs when in `GameState.Playing`.
- Calls `GameManager.Instance.GameOver()` when the grid is full and no more matches are available.
- Hearts and score are also managed via `GameManager`.

---

## Summary of Key Features

- **Auto stacking of balls:** Balls settle and are stacked automatically into grid slots.
- **Matching & clearing:** Detects 3+ in a row (all directions), clears them with animation, and adds score.
- **Lid closing:** Prevents overfilling. Lids open again if a match clears space.
- **Ball stuck detection:** Player loses heart if ball doesn't move on top of a closed lid.
- **Order flow:** Drop → Stack → Check match → Remove match → Add score → Update lids → Shift → Check full → Check lose.
- **Replayability:** All balls and state can be reset for a new game without reloading the scene.
- **Smooth UX:** DOTween is used for all transitions.
- **Low-friction walls:** Ensures balls smoothly enter columns.

---

## Example Usage

- Attach TubeManager to an empty GameObject.
- Assign references in the Inspector (columns, rows, ballLayerMask, etc.).
- Place columns and lids in the scene. Set ball colliders and wall friction as described.
- Start the game from GameManager. Balls will auto-stack, match, and clear as described above.

---

## See Also

- `GridChecker`: For match-3 detection logic.
- `LidsController`: For managing open/close lid states.
- `GameManager`: For game state, score, and heart management.
- `BallSpawner`: For spawning and queueing new balls.

---