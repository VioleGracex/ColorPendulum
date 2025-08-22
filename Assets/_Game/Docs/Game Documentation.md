# Game Documentation

## Overview

This game is a physics-based, match-3 puzzle game where colored balls drop into columns (tubes) and stack into a grid.  
Players launch balls via a swinging pendulum, aiming to create matches of 3 or more of the same color (vertically, horizontally, or diagonally) to clear them for points.  
Strategic placement, timing, and awareness of tube lids and hearts/lives are key to maximizing score and surviving as long as possible.

---

## Key Systems and Scripts

### 1. **GameManager**
- **Purpose:** Central controller for game state, score, hearts (lives), and scene transitions.
- **Responsibilities:**
  - Tracks and updates score and hearts.
  - Handles `Playing`, `GameOver`, and `Cleaning` states.
  - Responds to UI button clicks (start, replay, menu).
  - Triggers end-of-game events and UI.

---

### 2. **BallSpawner**
- **Purpose:** Handles the spawning, queueing, and preview of upcoming balls.
- **Features:**
  - Maintains a fair, randomized pool of ball colors, preventing 3-in-a-row of the same color.
  - Always keeps 3 balls in the "next balls" UI preview, refilling the pool every 3 balls used.
  - Animates spawning and launching of each ball.
  - Handles user input for breaking/releasing the ball from the pendulum.
  - Tracks spawned colors for analytics or replay.
  - Penalizes the player (heart loss) if a ball is not placed in a tube in time.

---

### 3. **PendulumController**
- **Purpose:** Controls the swinging pendulum, to which new balls are attached before launch.
- **Features:**
  - Animates a realistic swinging motion.
  - Handles attachment and detachment of balls.
  - Launches balls with variable force based on swing direction.

---

### 4. **TubeManager**
- **Purpose:** Manages the core puzzle grid, stacking, matching, clearing, and game-over logic.
- **Features:**
  - Detects when balls come to rest inside tubes and auto-stacks them in the grid.
  - Checks for matches in all directions after each placement.
  - Clears matched balls with smooth DOTween animations and score multipliers.
  - Triggers cascading clears for chain reactions.
  - Updates and animates tube lids (open/closed) when columns are full or cleared.
  - Detects and penalizes balls stuck on closed lids (heart loss).
  - Allows all balls and state to be cleared for replayability without reloading the scene.
  - Uses low-friction physics on tube walls for smooth ball movement.

---

### 5. **LidsController**
- **Purpose:** Handles the opening and closing of each tube's lid.
- **Features:**
  - Visually and physically closes a column when full (but not matching).
  - Reopens the lid when space is cleared.

---

### 6. **UIManager**
- **Purpose:** Manages all game UI, including panels, score, hearts, ball previews, and menus.
- **Features:**
  - Animates and displays next balls preview using DOTween.
  - Shows/hides main menu, game, and game over panels.
  - Updates score and hearts in real time.
  - Animates tube and ball UI elements for feedback.
  - Handles UI button events and transitions.

---

### 7. **Ball**
- **Purpose:** Represents an individual ball in the grid.
- **Features:**
  - Stores color and placement state.
  - Handles PlayClearEffect animation when matched/cleared.
  - Communicates with TubeManager for grid logic.

---

### 8. **GridChecker**
- **Purpose:** Utility for checking matches in the grid.
- **Features:**
  - Returns all positions of connected balls matching the color of a newly placed ball.
  - Used by TubeManager after each placement and after cascades.

---

## Gameplay Flow

1. **Start Game:**  
   - Main menu appears. Player clicks to start.
   - Tubes animate into place, first ball appears swinging on the pendulum.

2. **Spawning and Launching Balls:**
   - The next 3 balls are shown in the preview UI.
   - Player releases the ball with a tap/click at their chosen time.

3. **Stacking and Matching:**
   - Ball falls into a tube, comes to rest, and auto-stacks into the lowest empty slot.
   - TubeManager checks for matches in all directions.
   - If a match is found, all matched balls are cleared with animation, score is awarded (with multipliers for larger matches).
   - If no match and the tube is full, the lid closes.

4. **Cascading and Shifting:**
   - After clearing, balls above shift down with animation.
   - Chain reactions are detected and resolved until no more matches.

5. **Lives and Penalties:**
   - If a ball is not placed in a tube in time, or gets stuck on a closed lid, it is destroyed and the player loses a heart.
   - Game ends when hearts reach zero or the grid is full with no possible matches.

6. **Game Over and Replay:**
   - Game over panel and score appear.
   - Player can replay without reloading the scene (TubeManager and BallSpawner provide full cleanup/reset).

---

## Design Decisions & Solutions

**Q: How do you ensure there are always 3 balls in the UI preview?**  
A: BallSpawner manages a queue, refilling it every 3 balls used and always topping up to 3 for UI. New balls are generated with a fair color distribution and no 3-in-a-row.

**Q: How do you prevent impossible or unfair ball sequences?**  
A: The ball pool is shuffled so no 3 of the same color are ever in a row, both for initial pool and refills.

**Q: What if a player tries to overfill a tube?**  
A: If a tube is full and not matching, its lid closes. Balls dropped onto the lid and left there too long cost the player a heart.

**Q: How do you keep the game feeling lively and responsive?**  
A: DOTween is used for all ball and UI animations, including stacking, clearing, shifting, and panel transitions.

**Q: How do you handle replayability?**  
A: Both TubeManager and BallSpawner provide clear methods to destroy all balls, reset all state, and start a new game without reloading the Unity scene.

**Q: How is the main game loop structured?**  
A:  
1. Drop →  
2. Place/Stack →  
3. Check match →  
4. Remove match →  
5. Add score →  
6. Update lids →  
7. Shift down →  
8. Check full grid →  
9. Check for lose/game over.

**Q: How do you ensure smooth physics for balls entering tubes?**  
A: Tube walls have zero friction, so balls slide in smoothly without sticking.

---

## Extending and Customizing

- **Grid size:** Change `columns` and `rows` in TubeManager for different puzzle sizes.
- **Ball colors:** Add more to `BallColor` enum and update color assignment logic.
- **Refill/queue logic:** Adjust pool refill size or game over conditions for different difficulty.
- **Special balls/effects:** Add new Ball types and handle them in TubeManager and BallSpawner.
- **UI/UX polish:** Further customize DOTween animations in UIManager for more feedback.

---

## Dependencies

- **DOTween:** For all movement and UI animations.
- **NaughtyAttributes (optional):** For dev/testing utilities in the Inspector.
- **Unity Input System:** For touch/mouse input.
- **Unity Physics2D:** For ball movement and collision.

---

## See Also

- [DOTween Documentation](http://dotween.demigiant.com/documentation.php)
- Unity Manual: [Physics2D](https://docs.unity3d.com/Manual/Physics2D.html)
- Unity Manual: [Input System](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.0/manual/index.html)

---